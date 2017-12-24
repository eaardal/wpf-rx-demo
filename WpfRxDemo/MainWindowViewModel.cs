using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ReactiveUI;
using ReactiveUI.Legacy;
using ReactiveCommand = ReactiveUI.ReactiveCommand;

namespace WpfRxDemo
{
    class MainWindowViewModel : ReactiveObject
    {
        private string _input;

        public string Input
        {
            get => _input;
            set => this.RaiseAndSetIfChanged(ref _input, value);
        }

        private readonly ObservableAsPropertyHelper<List<string>> _items;
        public List<string> Items => _items.Value;

        private readonly ObservableAsPropertyHelper<Visibility> _isProcessingInput;
        public Visibility IsProcessingInput => _isProcessingInput.Value;

        private readonly ObservableAsPropertyHelper<bool> _isInputFieldEnabled;
        public bool IsInputFieldEnabled => _isInputFieldEnabled.Value;

        private readonly ObservableAsPropertyHelper<Visibility> _isItemsVisible;
        public Visibility IsItemsVisible => _isItemsVisible.Value;

        public ReactiveCommand<string, string> ProcessInput { get; }

        public ReactiveCommand<Unit, Unit> FocusInputField { get; }

        public MainWindowViewModel(MainWindow view)
        {
            // Create command that sets focus for the "Input" TextBox in GUI
            FocusInputField = ReactiveCommand.Create(() =>
            {
                view.Input?.Focus();
                return Unit.Default;
            });

            // Create command that runs a task whenever executed
            ProcessInput = ReactiveCommand.CreateFromTask<string, string>(DoSomeWorkWithInput);

            // Set focus for the "Input" TextBox in GUI whenever the ProcessInput command has finished
            ProcessInput
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async _ =>
                {
                    await FocusInputField.Execute();
                });

            // When the "Input" TextBox changes value...
            this.WhenAnyValue(viewModel => viewModel.Input)
                // Wait some ms to ensure the user has stopped typing
                .Throttle(TimeSpan.FromMilliseconds(600), RxApp.MainThreadScheduler)
                // Validate
                .Where(value => !string.IsNullOrEmpty(value))
                // Normalize
                .Select(searchTerm => searchTerm.Trim())
                // Only listen for new values
                .DistinctUntilChanged()
                // Invoke command with new value
                .InvokeCommand(ProcessInput);
            
            // Set "IsProcessingInput" to Visible when the "ProcessInput" command is executing
            _isProcessingInput = ProcessInput.IsExecuting
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.IsProcessingInput, Visibility.Collapsed);

            // Disable the "Input" TextBox when the "ProcessInput" command is executing
            _isInputFieldEnabled = ProcessInput.IsExecuting
                .Select(x => !x)
                .ToProperty(this, x => x.IsInputFieldEnabled, true);

            // Put the processed item in a list and assign it to the Items property
            _items = ProcessInput
                .Select(val => new List<string>(Items){val})
                .ToProperty(this, x => x.Items, new List<string>());

            // Show the Items-list when there are items in the list
            _isItemsVisible = this.WhenAnyValue(x => x.Items)
                .Select(x => x.Any() ? Visibility.Visible : Visibility.Collapsed)
                .ToProperty(this, x => x.IsItemsVisible, Visibility.Collapsed);
            
            view.Activated += async (sender, args) => await ViewOnActivated(sender, args);
        }

        private async Task ViewOnActivated(object sender, EventArgs eventArgs)
        {
            // Set focus for the Input TextBox when the view is activated on startup
            await FocusInputField.Execute();
        }

        private static async Task<string> DoSomeWorkWithInput(string input)
        {
            await Task.Delay(1000);
            return await Task.FromResult($"Processed \"{input}\"");
        }
    }
}
