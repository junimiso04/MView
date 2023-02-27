﻿using Caliburn.Micro;
using MView.Utilities;
using MView.Utilities.Indexing;
using MView.Utilities.Text;
using MView.ViewModels.Dialogs;
using Ookii.Dialogs.Wpf;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace MView.ViewModels
{
    public class MainViewModel : Screen
    {
        private ExplorerViewModel _explorer = IoC.Get<ExplorerViewModel>();

        public ExplorerViewModel Explorer
        {
            get => _explorer;
            set => Set(ref _explorer, value);
        }

        private ViewerViewModel _viewer = IoC.Get<ViewerViewModel>();

        public ViewerViewModel Viewer
        {
            get => _viewer;
            set => Set(ref _viewer, value);
        }

        private ControllerViewModel _controller = IoC.Get<ControllerViewModel>();

        public ControllerViewModel Controller
        {
            get => _controller;
            set => Set(ref _controller, value);
        }

        private Settings _settings = IoC.Get<Settings>();

        public Settings Settings
        {
            get => _settings;
            set => Set(ref _settings, value);
        }

        public void Exit()
        {
            Environment.Exit(0);
        }

        public void LaunchGitHub()
        {
            Process.Start("https://github.com/handbros/mview");
        }

        public void LaunchGuide()
        {
            Process.Start("https://github.com/handbros/mview/blob/main/docs/GUIDE.md");
        }

        public async void LaunchInformation()
        {
            InformationViewModel viewModel = IoC.Get<InformationViewModel>();
            await IoC.Get<IWindowManager>().ShowDialogAsync(viewModel).ConfigureAwait(false);
        }
    }
}
