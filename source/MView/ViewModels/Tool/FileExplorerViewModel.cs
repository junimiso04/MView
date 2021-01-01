﻿using MView.Bases;
using MView.Commands;
using MView.Core;
using MView.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MView.ViewModels.Tool
{
    public class FileExplorerViewModel : ToolViewModelBase
    {
        #region ::Fields::

        public const string ToolContentId = "FileExplorer";

        private ObservableCollection<DirectoryItem> _nodes = new ObservableCollection<DirectoryItem>();
        private ObservableCollection<DirectoryItem> _selectedNodes = new ObservableCollection<DirectoryItem>();

        private ICommand _refreshCommand;

        #endregion

        #region ::Constructors::

        public FileExplorerViewModel() : base("File Explorer")
        {
            ContentId = ToolContentId;
        }

        #endregion

        #region ::Properties::

        public ObservableCollection<DirectoryItem> Nodes
        {
            get
            {
                return _nodes;
            }
            set
            {
                _nodes = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<DirectoryItem> SelectedNodes
        {
            get
            {
                return _selectedNodes;
            }
            set
            {
                _selectedNodes = value;
                RaisePropertyChanged();
            }
        }

        public ICommand RefreshCommand
        {
            get
            {
                return (_refreshCommand) ?? (_refreshCommand = new DelegateCommand(OnRefresh));
            }
        }

        #endregion

        #region ::Methods::

        public List<DirectoryItem> RefreshNodes(IEnumerable<DirectoryItem> nodes)
        {
            try
            {
                List<DirectoryItem> currentItems = nodes.ToList();
                List<DirectoryItem> newItems = new List<DirectoryItem>();

                foreach (DirectoryItem item in currentItems)
                {
                    if (item.Type == DirectoryItemType.BaseDirectory)
                    {
                        newItems.Add(RefreshItem(item));
                    }
                    else
                    {
                        throw new InvalidOperationException("Only top-level base directories can be refreshed.");
                    }
                }

                Workspace.Instance.Report.AddReportWithIdentifier("Nodes of FileExplorer have been refreshed.", ReportType.Completed);
                return newItems;
            }
            catch (Exception ex)
            {
                Workspace.Instance.Report.AddReportWithIdentifier($"{ex.Message}\r\n{ex.StackTrace}", ReportType.Warning);
                return null;
            }
        }

        private DirectoryItem RefreshItem(DirectoryItem originalItem)
        {
            DirectoryItem newItem = new DirectoryItem();
            newItem.Icon = originalItem.Icon;
            newItem.Type = originalItem.Type;
            newItem.Name = originalItem.Name;
            newItem.FullName = originalItem.FullName;
            newItem.IsExpanded = originalItem.IsExpanded;
            newItem.IsSelected = originalItem.IsSelected;
            newItem.SubItems = new List<DirectoryItem>();

            if (originalItem.Type == DirectoryItemType.BaseDirectory || originalItem.Type == DirectoryItemType.Directory)
            {
                if (Directory.Exists(originalItem.FullName))
                {
                    // Recollect directories.
                    foreach (DirectoryItem item in originalItem.SubItems)
                    {
                        if (item.Type == DirectoryItemType.BaseDirectory || item.Type == DirectoryItemType.Directory)
                        {
                            if (Directory.Exists(item.FullName))
                            {
                                newItem.SubItems.Add(RefreshItem(item));
                            }
                        }
                    }

                    // Recollect new directories.
                    List<string> oldDirectories = new List<string>();

                    foreach (DirectoryItem item in originalItem.SubItems)
                    {
                        oldDirectories.Add(item.FullName);
                    }

                    var directories = new DirectoryInfo(originalItem.FullName).EnumerateDirectories();

                    foreach (DirectoryInfo subdir in directories)
                    {
                        if (!oldDirectories.Contains(subdir.FullName))
                        {
                            newItem.SubItems.Add(new DirectoryItem(subdir));
                        }
                    }

                    // Recollect files.
                    var files = new DirectoryInfo(originalItem.FullName).EnumerateFiles();

                    foreach (FileInfo file in files)
                    {
                        newItem.SubItems.Add(new DirectoryItem(file));
                    }
                }
            }
            else if (originalItem.Type == DirectoryItemType.File)
            {
                throw new InvalidOperationException("The file cannot be a refresh target.");
            }

            return newItem;
        }

        public string GetBaseDirectory(List<DirectoryItem> items, string filePath)
        {
            foreach (DirectoryItem item in items)
            {
                if (item.Type == DirectoryItemType.BaseDirectory)
                {
                    if (IsBaseOf(filePath, item.FullName))
                    {
                        return item.FullName;
                    }
                }
            }

            return null;
        }

        private bool IsBaseOf(string target, string candidate)
        {
            target = Path.GetFullPath(target);
            candidate = Path.GetFullPath(candidate);

            if (target.Equals(candidate, StringComparison.OrdinalIgnoreCase) || target.StartsWith(candidate + "\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public List<string> GetSelectedFiles(List<DirectoryItem> nodes, List<string> extensions = null)
        {
            // Get directories and files.
            List<DirectoryItem> directories = new List<DirectoryItem>();
            List<DirectoryItem> files = new List<DirectoryItem>();
            List<string> result = new List<string>();

            foreach (DirectoryItem item in nodes)
            {
                if (item.Type == DirectoryItemType.BaseDirectory || item.Type == DirectoryItemType.Directory)
                {
                    directories.Add(item);
                }
                else
                {
                    files.Add(item);
                }
            }

            // Ignore duplicated directories.
            for (int i = 0; i < directories.Count; i++)
            {
                foreach (DirectoryItem item in directories)
                {
                    if (IsBaseOf(directories[i].FullName, item.FullName) && directories.IndexOf(directories[i]) != i)
                    {
                        directories.RemoveAt(i);
                        break;
                    }
                }
            }

            // Ignore duplicated files.
            for (int i = 0; i < files.Count; i++)
            {
                foreach (DirectoryItem item in directories)
                {
                    if (IsBaseOf(files[i].FullName, item.FullName) && files.IndexOf(files[i]) != i)
                    {
                        files.RemoveAt(i);
                        break;
                    }
                }
            }

            // Get subitems.
            foreach (DirectoryItem item in directories)
            {
                result.AddRange(FileManager.GetFiles(item.FullName, extensions));
            }

            foreach (DirectoryItem item in files)
            {
                result.Add(item.FullName);
            }

            return result;
        }

        public Dictionary<string, string> IndexFromList(List<string> files, string baseDirectory, string saveDirectory)
        {
            try
            {
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                Dictionary<string, string> fileList = new Dictionary<string, string>();

                foreach (string file in files)
                {
                    string relativePath = Path.GetRelativePath(baseDirectory, file);

                    if (relativePath != file)
                    {
                        string savePath = Path.Combine(saveDirectory, relativePath);
                        fileList.Add(file, savePath);
                    }
                }

                return fileList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region ::Command Actions::

        private void OnRefresh()
        {
            Nodes = new ObservableCollection<DirectoryItem>(RefreshNodes(_nodes));
            SelectedNodes = new ObservableCollection<DirectoryItem>();
        }

        #endregion
    }
}
