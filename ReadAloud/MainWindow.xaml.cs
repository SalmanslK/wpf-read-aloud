using System.IO;
using System.Speech.Synthesis;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ReadAloud;

public partial class MainWindow : Window
{
    private readonly SpeechSynthesizer _synth = new();
    private string? _pdfPath;

    // Where in the text box the current reading started; speech positions are relative to it.
    private int _readOffset;

    public MainWindow()
    {
        InitializeComponent();

        _synth.SetOutputToDefaultAudioDevice();
        VoiceBox.ItemsSource = _synth.GetInstalledVoices()
            .Where(voice => voice.Enabled)
            .Select(voice => voice.VoiceInfo.Name)
            .ToList();
        VoiceBox.SelectedItem = _synth.Voice.Name;

        // The synthesizer raises its events on a worker thread.
        _synth.SpeakProgress += (_, e) => Dispatcher.BeginInvoke(() => HighlightWord(e.CharacterPosition, e.CharacterCount));
        _synth.StateChanged += (_, _) => Dispatcher.BeginInvoke(UpdateControls);
        _synth.SpeakCompleted += (_, _) => Dispatcher.BeginInvoke(UpdateControls);
        Closed += (_, _) => _synth.Dispose();

        UpdateControls();
    }

    private void OpenPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf" };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var pages = PdfText.PageCount(dialog.FileName);
            _pdfPath = dialog.FileName;
            FromPageBox.Text = "1";
            ToPageBox.Text = pages.ToString();
            PageCountText.Text = $"of {pages}";
            Title = $"Read Aloud — {Path.GetFileName(_pdfPath)}";
            LoadPages();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not open the PDF.\n\n{ex.Message}", "Read Aloud", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        UpdateControls();
    }

    private void LoadPages_Click(object sender, RoutedEventArgs e) => LoadPages();

    private void LoadPages()
    {
        if (_pdfPath is null)
        {
            return;
        }

        StopReading();
        var from = int.TryParse(FromPageBox.Text, out var f) ? f : 1;
        var to = int.TryParse(ToPageBox.Text, out var t) ? t : from;
        ReadingText.Text = PdfText.Extract(_pdfPath, from, to);
        ReadingText.CaretIndex = 0;
    }

    private void Speak_Click(object sender, RoutedEventArgs e)
    {
        var text = ReadingText.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        StopReading();

        // Read from the cursor (snapped back to the start of its word), or from the top.
        _readOffset = ReadingText.CaretIndex < text.Length ? ReadingText.CaretIndex : 0;
        while (_readOffset > 0 && !char.IsWhiteSpace(text[_readOffset - 1]))
        {
            _readOffset--;
        }

        if (VoiceBox.SelectedItem is string voice)
        {
            _synth.SelectVoice(voice);
        }
        _synth.Rate = (int)RateSlider.Value;
        _synth.SpeakAsync(text[_readOffset..]);
        UpdateControls();
    }

    private void Pause_Click(object sender, RoutedEventArgs e) => _synth.Pause();

    private void Resume_Click(object sender, RoutedEventArgs e) => _synth.Resume();

    private void Stop_Click(object sender, RoutedEventArgs e) => StopReading();

    private void StopReading()
    {
        // A paused synthesizer ignores cancellation until it is resumed.
        if (_synth.State == SynthesizerState.Paused)
        {
            _synth.Resume();
        }
        _synth.SpeakAsyncCancelAll();
    }

    private void HighlightWord(int position, int length)
    {
        var start = _readOffset + position;
        if (start + length > ReadingText.Text.Length)
        {
            return;
        }
        ReadingText.Select(start, length);
        ReadingText.ScrollToLine(ReadingText.GetLineIndexFromCharacterIndex(start));
    }

    private void ReadingText_TextChanged(object sender, TextChangedEventArgs e) => UpdateControls();

    private void UpdateControls()
    {
        // Called from the constructor's TextChanged before every control exists.
        if (StatusText is null)
        {
            return;
        }

        var state = _synth.State;
        SpeakButton.IsEnabled = !string.IsNullOrWhiteSpace(ReadingText.Text);
        PauseButton.IsEnabled = state == SynthesizerState.Speaking;
        ResumeButton.IsEnabled = state == SynthesizerState.Paused;
        StopButton.IsEnabled = state != SynthesizerState.Ready;
        FromPageBox.IsEnabled = ToPageBox.IsEnabled = LoadPagesButton.IsEnabled = _pdfPath is not null;
        StatusText.Text = state switch
        {
            SynthesizerState.Speaking => "Reading…",
            SynthesizerState.Paused => "Paused",
            _ => "Ready",
        };
    }
}
