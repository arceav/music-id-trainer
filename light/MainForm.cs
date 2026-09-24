using System.Drawing;
using System.IO;
using System.Text.Json;

namespace MusicIdTrainer;

internal sealed class MainForm : System.Windows.Forms.Form
{
    private sealed record Track(string FileName, string Path)
    {
        public string Name => System.IO.Path.GetFileNameWithoutExtension(FileName)
            .Replace('_', ' ').Replace('-', ' ').Trim();
        public override string ToString() => Name;
    }

    private enum QuizMode { Identify, Guide }

    private readonly List<Track> _tracks = [];
    private readonly ClipPlayer _player = new();
    private readonly ListBox _recordings = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown _seconds = new() { Minimum = 1, Maximum = 120, Value = 5, Dock = DockStyle.Fill };
    private readonly NumericUpDown _choiceCount = new() { Minimum = 2, Maximum = 6, Value = 4, Dock = DockStyle.Fill };
    private readonly ComboBox _startMode = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly Label _packStatus = new() { Dock = DockStyle.Fill, AutoEllipsis = true };
    private readonly Label _score = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };
    private readonly Label _question = new() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 16, FontStyle.Bold) };
    private readonly Label _status = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _choices = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly TextBox _feedback = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical, TabStop = false };
    private readonly Button _identifyButton = new() { Text = "Identify pieces", AutoSize = true };
    private readonly Button _guideButton = new() { Text = "Listening guide", AutoSize = true };
    private readonly Button _replayButton = new() { Text = "Replay clip", AutoSize = true, Enabled = false };
    private readonly Button _revealButton = new() { Text = "Reveal answer", AutoSize = true, Enabled = false };
    private readonly Button _nextButton = new() { Text = "Next question", AutoSize = true, Visible = false };

    private QuestionPack? _pack;
    private QuizMode? _mode;
    private List<QuestionCard> _deck = [];
    private Track? _current;
    private Track? _last;
    private QuestionCard? _card;
    private List<string> _options = [];
    private int _correctIndex;
    private int _asked;
    private int _correct;
    private bool _answered;

    public MainForm()
    {
        Text = "Music ID Trainer";
        Font = new Font("Segoe UI", 10);
        MinimumSize = new Size(900, 700);
        ClientSize = new Size(1100, 780);
        StartPosition = FormStartPosition.CenterScreen;

        _startMode.Items.AddRange(["Random position", "Beginning"]);
        _startMode.SelectedIndex = 0;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), RowCount = 2, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(new Label { Text = "Music ID Trainer", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 19, FontStyle.Bold) }, 0, 0);

        var split = new SplitContainer { Dock = DockStyle.Fill, Size = new Size(1050, 650), SplitterDistance = 370 };
        split.Panel1MinSize = 330;
        split.Panel2MinSize = 450;
        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 47));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 53));
        left.Controls.Add(CreateRecordingsPanel(), 0, 0);
        left.Controls.Add(CreatePracticePanel(), 0, 1);
        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(CreateQuizPanel());
        root.Controls.Add(split, 0, 1);
        Controls.Add(root);

        _identifyButton.Click += (_, _) => StartIdentify();
        _guideButton.Click += (_, _) => StartGuide();
        _replayButton.Click += (_, _) => PlayCurrent(true);
        _revealButton.Click += (_, _) => Answer(null);
        _nextButton.Click += (_, _) => NextQuestion();
        _choices.Resize += (_, _) => ResizeChoices();
        FormClosed += (_, _) => _player.Dispose();
        UpdatePackStatus();
        _question.Text = "Load recordings, then choose a mode.";
    }

    private GroupBox CreateRecordingsPanel()
    {
        var box = new GroupBox { Text = "1. Load recordings", Dock = DockStyle.Fill, Padding = new Padding(10) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        content.Controls.Add(_recordings, 0, 0);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, AutoScroll = true };
        buttons.Controls.Add(ActionButton("Load MP3s…", LoadRecordings));
        buttons.Controls.Add(ActionButton("Remove", RemoveSelected));
        buttons.Controls.Add(ActionButton("Clear", ClearRecordings));
        content.Controls.Add(buttons, 0, 1);
        box.Controls.Add(content);
        return box;
    }

    private GroupBox CreatePracticePanel()
    {
        var box = new GroupBox { Text = "2. Practice", Dock = DockStyle.Fill, Padding = new Padding(10) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1 };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(Labeled("Clip length (seconds)", _seconds), 0, 0);
        content.Controls.Add(Labeled("ID answer choices", _choiceCount), 0, 1);
        content.Controls.Add(Labeled("ID excerpt position", _startMode), 0, 2);
        var modes = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        modes.Controls.Add(_identifyButton);
        modes.Controls.Add(_guideButton);
        content.Controls.Add(modes, 0, 3);
        content.Controls.Add(ActionButton("Load question pack (.json)…", LoadPack), 0, 4);
        content.Controls.Add(_packStatus, 0, 5);
        box.Controls.Add(content);
        return box;
    }

    private GroupBox CreateQuizPanel()
    {
        var box = new GroupBox { Text = "Quiz", Dock = DockStyle.Fill, Padding = new Padding(12) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 6, ColumnCount = 1 };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 105));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        content.Controls.Add(_question, 0, 0);
        content.Controls.Add(_status, 0, 1);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        actions.Controls.Add(_replayButton);
        actions.Controls.Add(_revealButton);
        content.Controls.Add(actions, 0, 2);
        content.Controls.Add(_choices, 0, 3);
        content.Controls.Add(_feedback, 0, 4);
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        footer.Controls.Add(_nextButton, 0, 0);
        footer.Controls.Add(_score, 1, 0);
        content.Controls.Add(footer, 0, 5);
        box.Controls.Add(content);
        return box;
    }

    private static Control Labeled(string text, Control control)
    {
        var row = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));
        row.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        row.Controls.Add(control, 1, 0);
        return row;
    }

    private static Button ActionButton(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true };
        button.Click += (_, _) => action();
        return button;
    }

    private void LoadRecordings()
    {
        using var dialog = new OpenFileDialog {
            Filter = "Audio files|*.mp3;*.wav;*.m4a;*.flac|All files|*.*",
            Multiselect = true,
            Title = "Select your recordings"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        foreach (var path in dialog.FileNames)
        {
            var name = Path.GetFileName(path);
            if (_tracks.Any(t => t.FileName.Equals(name, StringComparison.OrdinalIgnoreCase))) continue;
            _tracks.Add(new Track(name, path));
        }
        RefreshRecordings();
    }

    private void RefreshRecordings()
    {
        _recordings.Items.Clear();
        _recordings.Items.AddRange(_tracks.Cast<object>().ToArray());
        UpdatePackStatus();
    }

    private void RemoveSelected()
    {
        if (_recordings.SelectedItem is not Track track) return;
        _tracks.Remove(track);
        ResetQuiz();
        RefreshRecordings();
    }

    private void ClearRecordings()
    {
        _tracks.Clear();
        ResetQuiz();
        RefreshRecordings();
    }

    private void LoadPack()
    {
        using var dialog = new OpenFileDialog {
            Filter = "Question packs (*.json)|*.json|All files|*.*",
            Title = "Select a question pack",
            InitialDirectory = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "question-packs"))
                ? Path.Combine(AppContext.BaseDirectory, "question-packs") : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _pack = QuestionPack.Load(dialog.FileName);
            ResetQuiz();
            UpdatePackStatus();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            _packStatus.Text = $"Could not load question pack: {ex.Message}";
        }
    }

    private Track? FindTrack(string fileName) =>
        _tracks.FirstOrDefault(t => t.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));

    private List<QuestionCard> ReadyCards() => _pack is null ? [] :
        _pack.Cards.Where(c => FindTrack(c.File) is not null).ToList();

    private void UpdatePackStatus()
    {
        _packStatus.Text = _pack is null
            ? "Load a question pack and matching recordings for listening guide mode."
            : $"{_pack.Title}: {ReadyCards().Count} of {_pack.Cards.Count} questions ready.";
    }

    private void StartIdentify()
    {
        if (_tracks.Count < 2)
        {
            MessageBox.Show(this, "Load at least two recordings to identify pieces.");
            return;
        }
        BeginRound(QuizMode.Identify);
    }

    private void StartGuide()
    {
        if (_pack is null)
        {
            MessageBox.Show(this, "Load a question pack first.");
            return;
        }
        var available = ReadyCards();
        if (available.Count == 0)
        {
            MessageBox.Show(this, "Load the recordings named in the question pack first.");
            return;
        }
        Shuffle(available);
        _deck = available;
        BeginRound(QuizMode.Guide);
    }

    private void BeginRound(QuizMode mode)
    {
        _player.Stop();
        _mode = mode;
        _asked = _correct = 0;
        _last = null;
        _score.Text = "";
        NextQuestion();
    }

    private static void Shuffle<T>(IList<T> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private void NextQuestion()
    {
        _player.Stop();
        _answered = false;
        _feedback.Text = _status.Text = "";
        _nextButton.Visible = false;
        _choices.Controls.Clear();

        if (_mode == QuizMode.Guide)
        {
            if (_deck.Count == 0)
            {
                _question.Text = "Round complete";
                _status.Text = $"You got {_correct} of {_asked}. Choose a mode to practice again.";
                _current = null;
                _replayButton.Enabled = _revealButton.Enabled = false;
                return;
            }
            _card = _deck[0];
            _deck.RemoveAt(0);
            _current = FindTrack(_card.File);
            if (_current is null) { NextQuestion(); return; }
            _question.Text = _card.Question;
            _options = [.. _card.Choices];
            _correctIndex = _card.Answer;
        }
        else
        {
            var candidates = _tracks.Where(t => t != _last).ToList();
            _current = candidates[Random.Shared.Next(candidates.Count)];
            _last = _current;
            _card = null;
            _question.Text = "What piece is this?";
            var other = _tracks.Where(t => t != _current).ToList();
            Shuffle(other);
            var answers = other.Take(Math.Min(_tracks.Count, (int)_choiceCount.Value) - 1).ToList();
            answers.Add(_current);
            Shuffle(answers);
            _options = answers.Select(t => t.Name).ToList();
            _correctIndex = answers.IndexOf(_current);
        }

        for (var i = 0; i < _options.Count; i++)
        {
            var index = i;
            var button = new Button { Text = _options[i], Height = 46, Margin = new Padding(2, 3, 2, 3), TextAlign = ContentAlignment.MiddleLeft };
            button.Click += (_, _) => Answer(index);
            _choices.Controls.Add(button);
        }
        ResizeChoices();
        _replayButton.Enabled = _revealButton.Enabled = true;
        PlayCurrent(false);
    }

    private void ResizeChoices()
    {
        foreach (Control button in _choices.Controls)
            button.Width = Math.Max(150, _choices.ClientSize.Width - 24);
    }

    private void PlayCurrent(bool replay)
    {
        if (_current is null) return;
        var start = replay ? _player.LastStartSeconds : _mode == QuizMode.Guide ? _card!.Start : 0;
        var random = !replay && _mode == QuizMode.Identify && _startMode.SelectedIndex == 0;
        _player.Play(_current.Path, start, (int)_seconds.Value, random, message => _status.Text = message);
    }

    private void Answer(int? selected)
    {
        if (_answered || _current is null) return;
        _answered = true;
        _asked++;
        if (selected == _correctIndex) _correct++;
        _player.Stop();
        for (var i = 0; i < _choices.Controls.Count; i++)
        {
            var button = (Button)_choices.Controls[i];
            button.Enabled = false;
            button.UseVisualStyleBackColor = false;
            if (i == _correctIndex) button.BackColor = Color.LightGreen;
            else if (i == selected) button.BackColor = Color.LightCoral;
        }
        var prefix = selected == _correctIndex ? "Correct! " : $"Answer: {_options[_correctIndex]}. ";
        _feedback.Text = prefix + (_card?.Explanation ?? "");
        _score.Text = $"{_correct} correct / {_asked} answered";
        _nextButton.Text = _mode == QuizMode.Guide && _deck.Count == 0 ? "Finish round" : "Next question";
        _nextButton.Visible = true;
    }

    private void ResetQuiz()
    {
        _player.Stop();
        _mode = null;
        _current = _last = null;
        _card = null;
        _deck.Clear();
        _choices.Controls.Clear();
        _question.Text = "Load recordings, then choose a mode.";
        _status.Text = _score.Text = _feedback.Text = "";
        _replayButton.Enabled = _revealButton.Enabled = false;
        _nextButton.Visible = false;
    }
}
