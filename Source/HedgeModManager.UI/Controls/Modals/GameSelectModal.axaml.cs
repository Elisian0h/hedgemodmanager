using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HedgeModManager.CoreLib;
using HedgeModManager.UI.Models;
using HedgeModManager.UI.ViewModels;

namespace HedgeModManager.UI.Controls.Modals;

public partial class GameSelectModal : UserControl
{
    public bool IsUnleashedRecompiledMissing
    {
        get
        {
            var viewModel = DataContext as MainWindowViewModel;
            if (viewModel == null)
                return true;

            return !viewModel.Games.Any(x => x.Game.Name == "UnleashedRecompiled");
        }
    }

    public GameSelectModal()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnGameClick(object? sender, RoutedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not UIGame game ||
            DataContext is not MainWindowViewModel viewModel)
            return;
        viewModel.SelectedGame = game;
        if (viewModel.Modals.Count > 0)
            viewModel.Modals.RemoveAt(viewModel.Modals.Count - 1);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Preview
        if (DataContext == null && Design.IsDesignMode)
        {
            var viewModel = new MainWindowViewModel();
            var dummyGames = ModdableGameLocator.ModdableGameList
                .Select(gameInfo => new GameSimple("", gameInfo.ID, gameInfo.ID, "", null, "", null, null, null))
                .Select(x => new ModdableGameGeneric(x))
                .Cast<IModdableGame>();
            viewModel.Games = new(Games.GetUIGames(dummyGames));

            DataContext = viewModel;
        }
    }


    private async void OnAddGameClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new()
        {
            Title = "Select Game Executable...",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new("Executable Files")
                {
                    Patterns = ["*.exe"],
                    MimeTypes = ["application/x-msdownload"]
                }
            ]
        });

        if (files == null || files.Count == 0)
            return;

        string customPath = HedgeModManager.UI.Utils.ConvertToPath(files[0].Path);
        string executable = System.IO.Path.GetFileName(customPath);

        var matchedGameInfo = ModdableGameLocator.ModdableGameList.FirstOrDefault(gi =>
            gi.PlatformInfos.Values.SelectMany(v => v).Any(pi => System.IO.Path.GetFileName(pi.Executable).Equals(executable, StringComparison.OrdinalIgnoreCase)));

        if (matchedGameInfo == null)
        {
            var messageBox = new MessageBoxModal("Error", "The selected executable is not a supported game.");
            messageBox.AddButton("OK", (s, a) => messageBox.Close());
            messageBox.Open(viewModel);
            return;
        }

        if (!viewModel.Config.CustomGames.Contains(customPath, StringComparer.OrdinalIgnoreCase))
        {
            viewModel.Config.CustomGames.Add(customPath);
            await viewModel.Config.SaveAsync();

            if (this.VisualRoot is HedgeModManager.UI.Views.MainWindow mainWindow)
            {
                mainWindow.LoadGames();
                // Select the new game
                viewModel.SelectedGame = viewModel.Games.FirstOrDefault(x => x.Game.Root == System.IO.Path.GetDirectoryName(customPath) && x.Game.Name == matchedGameInfo.ID);
            }
        }
        else
        {
            var messageBox = new MessageBoxModal("Error", "This game is already added.");
            messageBox.AddButton("OK", (s, a) => messageBox.Close());
            messageBox.Open(viewModel);
            return;
        }

        if (viewModel.Modals.Count > 0)
            viewModel.Modals.RemoveAt(viewModel.Modals.Count - 1);
    }

    private void OnGameMissingPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (Helpers.IsFlatpak)
        {
            Utils.OpenURL("https://github.com/hedge-dev/HedgeModManager/issues/44");
        }
        else
        {
            var modal = new GameMissingInfoModal();
            if (DataContext is MainWindowViewModel viewModel)
                modal.Open(viewModel);
        }

    }
}