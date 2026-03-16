namespace Atc.Dsc.Configurations.Cli.Tui;

/// <summary>
/// Main TUI window with a two-panel split layout: profile list (left)
/// and detail view (right), plus a status bar at the bottom.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class MainWindow : Window
{
    private readonly IProfileRepository repository;
    private readonly IProfileParser parser;
    private readonly IDscClient dscClient;
    private readonly IApplication app;
    private readonly EnvironmentInfo envInfo;
    private readonly ExtensionLoader extensionLoader;

    private readonly ListView profileList;
    private readonly TabView detailTabs;
    private readonly ColoredOutputView overviewText;
    private readonly TextView resourcesText;
    private readonly TextView extensionsText;
    private readonly TextView rawYamlText;
    private readonly TextField filterField;
    private readonly ActionHintsView actionHints;
    private readonly LoadingSpinnerView spinner;

    private readonly ObservableCollection<string> profileDisplayNames = [];
    private readonly List<ProfileSummary> allProfileSummaries = [];
    private readonly List<int> filteredIndices = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="app">the Terminal.Gui application instance.</param>
    /// <param name="repository">the profile repository.</param>
    /// <param name="parser">the profile parser.</param>
    /// <param name="dscClient">the DSC client.</param>
    /// <param name="envInfo">the current environment info.</param>
    public MainWindow(
        IApplication app,
        IProfileRepository repository,
        IProfileParser parser,
        IDscClient dscClient,
        EnvironmentInfo envInfo)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentNullException.ThrowIfNull(dscClient);
        ArgumentNullException.ThrowIfNull(envInfo);

        this.app = app;
        this.repository = repository;
        this.parser = parser;
        this.dscClient = dscClient;
        this.envInfo = envInfo;
        extensionLoader = new ExtensionLoader(repository);

        Title = "atc-dsc - DSCv3 Configuration Manager";

        profileList = CreateProfileList();
        filterField = CreateFilterField();
        var leftPanel = CreateLeftPanel();

        overviewText = new ColoredOutputView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
        };

        resourcesText = CreateReadOnlyTextView(wordWrap: false);
        extensionsText = CreateReadOnlyTextView(wordWrap: false);
        rawYamlText = CreateReadOnlyTextView(wordWrap: false);
        detailTabs = CreateDetailTabs();
        spinner = new LoadingSpinnerView(app)
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(),
            Height = 1,
        };
        var rightPanel = CreateRightPanel(leftPanel);

        actionHints = CreateActionHints();

        Add(leftPanel, rightPanel, actionHints);
        AddKeyBindings();

        // Load profiles once the UI is ready
        Initialized += (_, _) => _ = RunGuardedAsync(LoadProfilesAsync);
    }

    private ListView CreateProfileList()
    {
        var list = new ListView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShowMarks = true,
            MarkMultiple = true,
            CanFocus = true,
        };

        list.SetScheme(DarkTheme.CreateListScheme());

        list.ValueChanged += OnProfileSelectionChanged;
        return list;
    }

    private TextField CreateFilterField()
    {
        var field = new TextField
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 1,
            Text = string.Empty,
        };

        field.TextChanged += (_, _) => ApplyFilter();
        return field;
    }

    private FrameView CreateLeftPanel()
    {
        var leftPanel = new FrameView
        {
            Title = "Profiles",
            X = 0,
            Y = 0,
            Width = Dim.Percent(40),
            Height = Dim.Fill(1),
        };

        leftPanel.Add(filterField, profileList);
        return leftPanel;
    }

    private static TextView CreateReadOnlyTextView(bool wordWrap) => new()
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        ReadOnly = true,
        WordWrap = wordWrap,
        CanFocus = false,
    };

    private TabView CreateDetailTabs()
    {
        var tabs = new TabView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
        };

        tabs.AddTab(new Tab { DisplayText = "Overview", View = overviewText }, andSelect: true);
        tabs.AddTab(new Tab { DisplayText = "Resources", View = resourcesText }, andSelect: false);
        tabs.AddTab(new Tab { DisplayText = "Extensions", View = extensionsText }, andSelect: false);
        tabs.AddTab(new Tab { DisplayText = "Raw YAML", View = rawYamlText }, andSelect: false);

        return tabs;
    }

    private FrameView CreateRightPanel(View leftPanel)
    {
        var rightPanel = new FrameView
        {
            Title = "Details",
            X = Pos.Right(leftPanel),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };

        rightPanel.Add(detailTabs, spinner);
        return rightPanel;
    }

    private ActionHintsView CreateActionHints()
    {
        var view = new ActionHintsView
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            Height = 1,
        };

        view.SetHints(BuildActionHints());
        return view;
    }

    private async Task LoadProfilesAsync()
    {
        spinner.Label = "Loading profiles...";
        spinner.Start();
        try
        {
            var summaries = await repository.ListProfilesAsync();
            allProfileSummaries.Clear();
            allProfileSummaries.AddRange(summaries);

            filteredIndices.Clear();
            profileDisplayNames.Clear();

            for (var i = 0; i < summaries.Count; i++)
            {
                profileDisplayNames.Add(summaries[i].Name);
                filteredIndices.Add(i);
            }

            await profileList.SetSourceAsync(profileDisplayNames);

            if (summaries.Count > 0)
            {
                profileList.SelectedItem = 0;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            overviewText.Clear();
            overviewText.AppendLine($"Error loading profiles: {ex.Message}", DarkTheme.Red);
        }
        finally
        {
            spinner.Stop();
        }
    }

    private void AddKeyBindings()
    {
        app.Keyboard.KeyDown += (_, key) =>
        {
            if (app.TopRunnableView != this)
            {
                return;
            }

            if (key == Key.Esc)
            {
                if (filterField.HasFocus)
                {
                    profileList.SetFocus();
                }
                else
                {
                    ConfirmQuit();
                }

                key.Handled = true;
                return;
            }

            if (filterField.HasFocus)
            {
                return;
            }

            HandleNonFilterKey(key);
        };
    }

    private void HandleNonFilterKey(Key key)
    {
        if (key == Key.Q)
        {
            ConfirmQuit();
            key.Handled = true;
        }
        else if (key == Key.Tab)
        {
            HandleTabKey();
            key.Handled = true;
        }
        else if (key == Key.H)
        {
            profileList.SetFocus();
            key.Handled = true;
        }
        else if (key == Key.L)
        {
            detailTabs.SetFocus();
            key.Handled = true;
        }
        else if (key == Key.J)
        {
            HandleJKey();
            key.Handled = true;
        }
        else if (key == Key.K)
        {
            HandleKKey();
            key.Handled = true;
        }
        else if (key == Key.G && !profileList.HasFocus)
        {
            HandleScrollToTop();
            key.Handled = true;
        }
        else if (key == Key.G.WithShift && !profileList.HasFocus)
        {
            HandleScrollToBottom();
            key.Handled = true;
        }
        else
        {
            HandleActionKeys(key);
        }
    }

    private void HandleTabKey()
    {
        if (profileList.HasFocus)
        {
            detailTabs.SetFocus();
        }
        else
        {
            profileList.SetFocus();
        }
    }

    private void HandleJKey()
    {
        if (profileList.HasFocus)
        {
            profileList.MoveDown();
        }
        else
        {
            ActiveDetailView()?.ScrollVertical(1);
        }
    }

    private void HandleKKey()
    {
        if (profileList.HasFocus)
        {
            profileList.MoveUp();
        }
        else
        {
            ActiveDetailView()?.ScrollVertical(-1);
        }
    }

    private void HandleScrollToTop()
    {
        var view = ActiveDetailView();
        view?.ScrollVertical(-view.GetContentSize().Height);
    }

    private void HandleScrollToBottom()
    {
        var view = ActiveDetailView();
        view?.ScrollVertical(view.GetContentSize().Height);
    }

    private void HandleActionKeys(Key key)
    {
        if (key == Key.A.WithCtrl)
        {
            SelectAll(selected: true);
            key.Handled = true;
        }
        else if (key == Key.D.WithCtrl)
        {
            SelectAll(selected: false);
            key.Handled = true;
        }
        else if (key == Key.T)
        {
            _ = RunGuardedAsync(() => ExecuteSelectedAsync(ExecutionMode.Test));
            key.Handled = true;
        }
        else if (key == Key.Enter)
        {
            _ = RunGuardedAsync(() => ExecuteSelectedAsync(ExecutionMode.Apply));
            key.Handled = true;
        }
        else if (key == '/')
        {
            FocusFilter();
            key.Handled = true;
        }
        else if (key == '?')
        {
            ShowHelp();
            key.Handled = true;
        }
    }

    private void ConfirmQuit()
    {
        var result = MessageBox.Query(
            app,
            "Quit",
            "Are you sure you want to exit?",
            "Cancel",
            "Quit");

        if (result == 1)
        {
            app.RequestStop();
        }
    }

    private View? ActiveDetailView() => detailTabs.SelectedTab?.View;

    private void SelectAll(bool selected)
    {
        profileList.MarkAll(selected);
        profileList.SetNeedsLayout();
        UpdateActionHints();
    }

    private void UpdateActionHints()
    {
        actionHints.SetHints(BuildActionHints());
    }

    private List<ActionHint> BuildActionHints()
    {
        var txt = DarkTheme.StatusBarKey;

        var list = new List<ActionHint>
        {
            new("Space", "Toggle", txt, txt),
            new("t", "Test", txt, txt),
        };

        if (envInfo.IsAdmin)
        {
            list.Add(new ActionHint("Enter", "Apply", txt, txt));
        }

        list.Add(new ActionHint("?", "Help", txt, txt));
        list.Add(new ActionHint("q", "Quit", txt, txt));

        var marked = profileList.GetAllMarkedItems();
        var count = marked.Count();
        if (count > 0)
        {
            list.Add(new ActionHint($"{count}", "selected", DarkTheme.StatusBarHighlight, txt));
        }

        if (!envInfo.IsAdmin)
        {
            list.Add(new ActionHint("\u2014", "test only", txt, txt));
        }

        return list;
    }

    private void OnProfileSelectionChanged(
        object? sender,
        ValueChangedEventArgs<int?> e)
    {
        var index = e.NewValue;
        if (index is not >= 0 || index.Value >= filteredIndices.Count)
        {
            return;
        }

        UpdateActionHints();

        _ = RunGuardedAsync(() => LoadProfileDetailAsync(filteredIndices[index.Value]));
    }

    private async Task LoadProfileDetailAsync(int index)
    {
        if (index < 0 || index >= allProfileSummaries.Count)
        {
            return;
        }

        var summary = allProfileSummaries[index];

        spinner.Label = $"Loading {summary.Name}...";
        spinner.Start();
        try
        {
            var content = await repository.GetProfileContentAsync(summary.FileName);
            var profile = parser.Parse(content, summary.FileName);

            PopulateOverviewTab(profile);
            PopulateResourcesTab(profile);

            // Extensions tab
            extensionsText.Text = await extensionLoader.LoadAsync(summary.FileName);

            // Raw YAML tab
            rawYamlText.Text = content;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            overviewText.Clear();
            overviewText.AppendLine($"Error loading profile: {ex.Message}", DarkTheme.Red);
        }
        finally
        {
            spinner.Stop();
        }
    }

    private void PopulateOverviewTab(Contracts.Profile profile)
    {
        overviewText.Clear();

        overviewText.AppendLine(profile.Name, DarkTheme.Header);
        overviewText.AppendLine(new string('\u2500', profile.Name.Length), DarkTheme.Dim);
        overviewText.AppendLine(string.Empty, DarkTheme.Default);

        if (profile.Description is not null)
        {
            overviewText.AppendLine(profile.Description, DarkTheme.Default);
            overviewText.AppendLine(string.Empty, DarkTheme.Default);
        }

        AppendResourceBreakdown(profile.Resources);

        overviewText.AppendLine(string.Empty, DarkTheme.Default);
        overviewText.AppendLine($"File: {profile.FileName}", DarkTheme.Dim);
        overviewText.AppendLine("Source: GitHub", DarkTheme.Dim);
    }

    private void AppendResourceBreakdown(IReadOnlyList<Resource> resources)
    {
        overviewText.AppendLine(
            $"Resources ({resources.Count})",
            DarkTheme.Header);

        var groups = GroupResourcesByType(resources);
        foreach (var (type, count) in groups)
        {
            var attr = GetTypeColor(type);
            var label = count > 1 ? PluralizeType(type) : type;
            overviewText.AppendLine($"  \u25a0 {count} {label}", attr);
        }
    }

    internal static IReadOnlyList<(string Type, int Count)> GroupResourcesByType(
        IReadOnlyList<Resource> resources)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var res in resources)
        {
            var type = AbbreviateType(res.Type);
            counts.TryGetValue(type, out var current);
            counts[type] = current + 1;
        }

        var result = new List<(string, int)>(counts.Count);
        foreach (var kvp in counts)
        {
            result.Add((kvp.Key, kvp.Value));
        }

        result.Sort((a, b) => b.Item2.CompareTo(a.Item2));
        return result;
    }

    internal static Terminal.Gui.Drawing.Attribute GetTypeColor(
        string abbreviatedType) => abbreviatedType switch
    {
        "Package" => DarkTheme.Green,
        "Script" => DarkTheme.Cyan,
        "PowerShell" => DarkTheme.Yellow,
        "VS Config" => DarkTheme.Blue,
        "Assertion" => DarkTheme.Dim,
        _ => DarkTheme.Default,
    };

    internal static string PluralizeType(string type) => type switch
    {
        "PowerShell" => "PowerShell",
        "VS Config" => "VS Configs",
        _ => type + "s",
    };

    private void PopulateResourcesTab(Contracts.Profile profile)
    {
        const string rowFmt = " {0,2}  {1,-37} {2,-13} {3}";
        var resList = profile.Resources;
        var sb = new StringBuilder();
        sb.AppendLine(string.Format(
            CultureInfo.InvariantCulture,
            rowFmt,
            "#",
            "Name",
            "Type",
            "Depends on"));
        sb.AppendLine(" " + new string('\u2500', 66));

        for (var i = 0; i < resList.Count; i++)
        {
            var res = resList[i];
            var name = res.Name.Length > 37 ? res.Name[..36] + "." : res.Name;
            var type = AbbreviateType(res.Type);
            var dependsOn = string.Empty;

            if (res.DependsOn.Count > 0)
            {
                var indices = new List<string>();
                foreach (var dep in res.DependsOn)
                {
                    var idx = ResolveDependencyIndex(dep, resList);
                    indices.Add(idx.HasValue ? $"#{idx.Value}" : "?");
                }

                dependsOn = "<- " + string.Join(", ", indices);
            }

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                rowFmt,
                i + 1,
                name,
                type,
                dependsOn));
        }

        resourcesText.Text = sb.ToString();
    }

    private Task ExecuteSelectedAsync(ExecutionMode executionMode)
    {
        if (executionMode == ExecutionMode.Apply &&
            !envInfo.IsAdmin)
        {
            MessageBox.Query(
                app,
                "Administrator Required",
                "Apply requires administrator privileges.\nPlease restart as admin, or use Test instead.",
                "OK");

            return Task.CompletedTask;
        }

        List<int> marked = [.. profileList.GetAllMarkedItems()];
        var indices = marked.Count > 0
            ? marked
            : profileList.SelectedItem.HasValue && profileList.SelectedItem.Value >= 0
                ? [profileList.SelectedItem.Value]
                : new List<int>();

        if (indices.Count == 0)
        {
            return Task.CompletedTask;
        }

        List<ProfileSummary> profiles = [.. indices.Select(i => allProfileSummaries[filteredIndices[i]])];

        if (!ConfirmExecution(executionMode, profiles))
        {
            return Task.CompletedTask;
        }

        var executionDialog = new ExecutionDialog(
            app,
            repository,
            dscClient,
            profiles,
            executionMode);

        app.Run(executionDialog);

        return Task.CompletedTask;
    }

    private bool ConfirmExecution(
        ExecutionMode executionMode,
        List<ProfileSummary> profiles)
    {
        if (executionMode != ExecutionMode.Apply)
        {
            return true;
        }

        const string modeLabel = "Apply";
        var names = string.Join(", ", profiles.Select(p => p.Name));
        var result = MessageBox.Query(
            app,
            $"Confirm {modeLabel}",
            $"Apply {profiles.Count} profile(s)?\n\n{names}",
            "Cancel",
            modeLabel);

        return result == 1;
    }

    private void ShowHelp()
    {
        const string helpText = """
                                Keyboard Shortcuts
                                ==================

                                Navigation:
                                  j / Down     Move down in profile list
                                  k / Up       Move up in profile list
                                  h / l        Switch to left / right panel
                                  Tab          Toggle panel focus

                                Selection:
                                  Space        Toggle profile selection
                                  Ctrl+a       Select all profiles
                                  Ctrl+d       Deselect all profiles

                                Actions:
                                  Enter        Apply selected profiles
                                  t            Test selected profiles
                                  /            Filter profiles
                                  Esc          Clear filter / Quit
                                  ?            Show this help

                                General:
                                  q            Quit
                                """;

        var trimmed = helpText.TrimEnd();
        var lineCount = trimmed.Split('\n').Length;

        var dialog = new Dialog
        {
            Title = "Help",
            Width = 52,
            Height = lineCount + 6,
        };

        var label = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = lineCount,
            Text = trimmed,
            CanFocus = false,
        };

        var okButton = new Button
        {
            X = Pos.Center(),
            Y = lineCount + 1,
            Text = "OK",
            IsDefault = true,
        };

        okButton.Accepting += (_, _) => app.RequestStop();

        dialog.Add(label, okButton);
        app.Run(dialog);
    }

    private void FocusFilter()
    {
        filterField.SetFocus();
    }

    private void ApplyFilter()
    {
        var filter = filterField.Text.Trim() ?? string.Empty;

        filteredIndices.Clear();
        profileDisplayNames.Clear();

        for (var i = 0; i < allProfileSummaries.Count; i++)
        {
            var name = allProfileSummaries[i].Name;

            if (filter.Length == 0 ||
                name.Contains(filter, StringComparison.OrdinalIgnoreCase))
            {
                filteredIndices.Add(i);
                profileDisplayNames.Add(name);
            }
        }

        profileList.SetSource(profileDisplayNames);

        if (profileDisplayNames.Count > 0)
        {
            profileList.SelectedItem = 0;
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Top-level guard for fire-and-forget async; must not leak unobserved exceptions")]
    private async Task RunGuardedAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown — ignore.
        }
        catch (Exception ex)
        {
            app.Invoke(() => MessageBox.Query(
                app,
                "Error",
                ex.Message,
                "OK"));
        }
    }

    internal static string AbbreviateType(string type) => type switch
    {
        "Microsoft.WinGet/Package" => "Package",
        "Microsoft.Windows/WindowsPowerShell" => "PowerShell",
        "Microsoft.DSC.Transitional/RunCommandOnSet" => "Script",
        "Microsoft.VisualStudio.DSC/VSComponents" => "VS Config",
        "Microsoft.DSC/Assertion" => "Assertion",
        _ => type.Contains('/', StringComparison.Ordinal)
            ? type[(type.IndexOf('/', StringComparison.Ordinal) + 1)..]
            : type,
    };

    internal static int? ResolveDependencyIndex(
        string dep,
        IReadOnlyList<Resource> resources)
    {
        // Extract resource name from: [resourceId('Type', 'Name')]
        var match = Regex.Match(
            dep,
            @"resourceId\('[^']+',\s*'(?<name>[^']+)'\)",
            RegexOptions.ExplicitCapture,
            TimeSpan.FromSeconds(1));

        if (!match.Success)
        {
            return null;
        }

        var name = match.Groups["name"].Value;
        for (var i = 0; i < resources.Count; i++)
        {
            if (resources[i].Name.Equals(name, StringComparison.Ordinal))
            {
                return i + 1;
            }
        }

        return null;
    }
}