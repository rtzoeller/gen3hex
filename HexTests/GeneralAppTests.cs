﻿using HavenSoft.Gen3Hex.Model;
using HavenSoft.Gen3Hex.ViewModel;
using System;
using System.Linq;
using System.Windows.Input;
using Xunit;

namespace HavenSoft.HexTests {
   public class GeneralAppTests {
      private readonly EditorViewModel editor;
      private readonly StubFileSystem fileSystem;

      public GeneralAppTests() {
         fileSystem = new StubFileSystem();
         editor = new EditorViewModel(fileSystem);
      }

      [Fact]
      public void ApplicationOpensWithNoTabs() {
         Assert.Equal(0, editor.Count);
         Assert.Equal(-1, editor.SelectedIndex);
      }

      [Fact]
      public void CanAddTabs() {
         var viewPort = new ViewPort();
         editor.Add(viewPort);
         Assert.Equal(1, editor.Count);
         Assert.Equal(0, editor.SelectedIndex);
      }

      [Fact]
      public void CanReorderTabs() {
         var viewPort1 = new ViewPort(new LoadedFile("file1", new byte[0]));
         var viewPort2 = new ViewPort(new LoadedFile("file2", new byte[0]));
         editor.Add(viewPort1);
         editor.Add(viewPort2);
         Assert.Equal(2, editor.Count);
         Assert.Equal(1, editor.SelectedIndex);

         editor.SwapTabs(0, 1);

         Assert.Equal("file2", editor[0].Name);
         Assert.Equal("file1", editor[1].Name);
         Assert.Equal(0, editor.SelectedIndex);
      }

      [Fact]
      public void UndoRunsForCurrentTab() {
         int executeCounter = 0;
         editor.Add(new StubTabContent());
         editor.Add(new StubTabContent {
            Undo = new StubCommand {
               CanExecute = arg => true,
               Execute = arg => executeCounter++
            },
         });

         editor.Undo.Execute();
         Assert.Equal(1, executeCounter);

         editor.SelectedIndex = 0;
         Assert.False(editor.Undo.CanExecute(null));
      }

      [Fact]
      public void SaveRunsForCurrentTab() {
         int executeCount = 0;
         editor.Add(new StubTabContent {
            Save = new StubCommand {
               CanExecute = arg => true,
               Execute = arg => executeCount += 1,
            },
         });
         editor.Add(new StubTabContent {
            Save = new StubCommand {
               CanExecute = arg => true,
               Execute = arg => executeCount += 10,
            },
         });

         editor.Save.Execute();
         Assert.Equal(10, executeCount);

         executeCount = 0;
         editor.SaveAll.Execute();
         Assert.Equal(11, executeCount);
      }

      [Fact]
      public void SaveAsRunsForCurrentTab() {
         int executeCount = 0;
         editor.Add(new StubTabContent {
            SaveAs = new StubCommand {
               CanExecute = arg => true,
               Execute = arg => executeCount += 1,
            },
         });
         editor.Add(new StubTabContent {
            SaveAs = new StubCommand {
               CanExecute = arg => true,
               Execute = arg => executeCount += 10,
            },
         });

         editor.SaveAs.Execute();
         Assert.Equal(10, executeCount);
      }

      [Fact]
      public void SaveAllSavesAllDocumentsThatNeedSaving() {
         int executeCount = 0;

         var save = new StubCommand { CanExecute = arg => true, Execute = arg => executeCount++ };
         var noSave = new StubCommand { CanExecute = arg => false, Execute = arg => executeCount++ };
         editor.Add(new StubTabContent { Save = save });
         editor.Add(new StubTabContent { Save = noSave });
         editor.Add(new StubTabContent { Save = save });

         editor.SaveAll.Execute();
         Assert.Equal(2, executeCount);
      }

      [Fact]
      public void ClosingCurrentTabSelectsAnotherTab() {
         editor.Add(CreateClosableTab());
         editor.Add(CreateClosableTab());
         editor.Add(CreateClosableTab());

         editor.Close.Execute();

         Assert.Equal(1, editor.SelectedIndex);
      }

      [Fact]
      public void ClosingAllTabsWorks() {
         editor.Add(CreateClosableTab());
         editor.Add(CreateClosableTab());
         editor.Add(CreateClosableTab());

         editor.CloseAll.Execute();

         Assert.Equal(-1, editor.SelectedIndex);
         Assert.Equal(0, editor.Count);
      }

      [Fact]
      public void NewAddsATab() {
         editor.New.Execute();

         Assert.Equal(1, editor.Count);
      }

      [Fact]
      public void OpenCanAddTab() {
         fileSystem.OpenFile = args => new LoadedFile("chosenFile.txt", new byte[20]);

         editor.Open.Execute();

         Assert.Equal(1, editor.Count);
      }

      [Fact]
      public void OpenDoesNotAddTabIfUserCancels() {
         fileSystem.OpenFile = args => null;

         editor.Open.Execute();

         Assert.Equal(0, editor.Count);
      }

      [Fact]
      public void SaveCommandsPassInFileSystemAsParameter() {
         int count = 0;
         void checkIfArgIsFileSystem(object arg) { if (arg is IFileSystem) count++; }
         bool canExecuteWrapper(object arg) { checkIfArgIsFileSystem(arg); return true; }
         var save = new StubCommand { CanExecute = canExecuteWrapper, Execute = checkIfArgIsFileSystem };

         editor.Add(new StubTabContent { Save = save, SaveAs = save });
         editor.Add(new StubTabContent { Save = save, SaveAs = save });
         editor.Add(new StubTabContent { Save = save, SaveAs = save });

         editor.Save.Execute(); // once
         editor.SaveAs.Execute(); // once
         editor.SaveAll.Execute(); // 6 times, since SaveAll should also check CanExecute

         Assert.Equal(8, count);
      }

      [Fact]
      public void CloseCommandsPassInFileSystemAsParameter() {
         int count = 0;
         void checkIfArgIsFileSystem(object arg) { if (arg is IFileSystem) count++; }
         bool canExecuteWrapper(object arg) { checkIfArgIsFileSystem(arg); return true; }
         var close = new StubCommand { CanExecute = canExecuteWrapper, Execute = checkIfArgIsFileSystem };

         editor.Add(new StubTabContent { Close = close });
         editor.Add(new StubTabContent { Close = close });
         editor.Add(new StubTabContent { Close = close });

         editor.Close.Execute(); // once
         editor.CloseAll.Execute(); // 6 times, since SaveAll should also check CanExecute

         Assert.Equal(7, count);
      }

      [Fact]
      public void EditorNotifiesCanExecuteChangedOnTabChange() {
         int count = 0;
         editor.Save.CanExecuteChanged += (sender, e) => count++;
         editor.SaveAs.CanExecuteChanged += (sender, e) => count++;
         editor.Close.CanExecuteChanged += (sender, e) => count++;
         editor.Undo.CanExecuteChanged += (sender, e) => count++;
         editor.Redo.CanExecuteChanged += (sender, e) => count++;
         var tab = new StubTabContent();
         tab.Close = new StubCommand { CanExecute = arg => true, Execute = arg => tab.Closed.Invoke(tab, EventArgs.Empty) };

         editor.Add(tab);
         Assert.Equal(5, count);

         count = 0;
         editor.Close.Execute();
         Assert.Equal(5, count);
      }

      [Fact]
      public void EditorNotifiesWhenUndoCanExecuteChange() {
         int count = 0;
         var undo = new StubCommand();
         var tab = new StubTabContent { Undo = undo };
         editor.Add(tab);
         editor.Undo.CanExecuteChanged += (sender, e) => count++;

         undo.CanExecute = arg => true;
         undo.CanExecuteChanged.Invoke(undo, EventArgs.Empty);

         Assert.Equal(1, count);
      }

      [Fact]
      public void SaveAllChangesWhenCurrentFileChanges() {
         int count = 0;
         var save = new StubCommand();
         var tab = new StubTabContent { Save = save };
         editor.Add(tab);
         editor.SaveAll.CanExecuteChanged += (sender, e) => count++;

         save.CanExecute = arg => true;
         save.CanExecuteChanged.Invoke(save, EventArgs.Empty);

         Assert.Equal(1, count);
      }

      [Fact]
      public void EditorShowsErrors() {
         var tab = new StubTabContent();
         editor.Add(tab);
         var clearErrorChangedNotifications = 0;
         editor.ClearError.CanExecuteChanged += (sender, e) => clearErrorChangedNotifications += 1;

         tab.OnError.Invoke(tab, "Some Message");

         Assert.True(editor.ShowError);
         Assert.Equal(editor.ErrorMessage, "Some Message");
         Assert.Equal(1, clearErrorChangedNotifications);
      }

      [Fact]
      public void EditorCanClearErrors() {
         var tab = new StubTabContent();
         editor.Add(tab);
         var clearErrorChangedNotifications = 0;
         editor.ClearError.CanExecuteChanged += (sender, e) => clearErrorChangedNotifications += 1;

         tab.OnError.Invoke(tab, "Some Message");
         editor.ClearError.Execute();

         Assert.False(editor.ShowError);
         Assert.Equal(editor.ErrorMessage, string.Empty);
         Assert.Equal(2, clearErrorChangedNotifications);
      }

      [Fact]
      public void ShowingGotoClearsErrors() {
         var tab = new StubTabContent();
         editor.Add(tab);

         tab.OnError.Invoke(tab, "Some Message");
         editor.ShowGoto.Execute(true);

         Assert.True(editor.GotoControlVisible);
         Assert.False(editor.ShowError);
      }

      private StubTabContent CreateClosableTab() {
         var tab = new StubTabContent();
         var close = new StubCommand { CanExecute = arg => true };
         close.Execute = arg => tab.Closed.Invoke(tab, EventArgs.Empty);
         tab.Close = close;
         return tab;
      }
   }
}
