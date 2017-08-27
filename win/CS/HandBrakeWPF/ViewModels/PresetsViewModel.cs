// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PresetsViewModel.cs" company="HandBrake Project (http://handbrake.fr)">
//   This file is part of the HandBrake source code - It may be used under the terms of the GNU General Public License.
// </copyright>
// <summary>
//   The Presets View Model
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace HandBrakeWPF.ViewModels
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using Caliburn.Micro;
    using HandBrake.ApplicationServices.Interop.Model;
    using HandBrake.ApplicationServices.Utilities;
    using HandBrakeWPF.Properties;
    using HandBrakeWPF.Services.Presets.Model;
    using HandBrakeWPF.Services.Scan.Model;
    using HandBrakeWPF.Utilities;
    using HandBrakeWPF.ViewModels.Interfaces;
    using Microsoft.Win32;
    using Services.Presets.Interfaces;
    using Services.Interfaces;
    using System.Windows;
    using Services.Encode.Model;
    using Model.Subtitles;
    using Model.Audio;
    using Factories;

    /// <summary>
    /// The Presets View Model
    /// </summary>
    public class PresetsViewModel : ViewModelBase, IPresetsViewModel
    {
        private readonly IWindowManager windowManager;
        private readonly IPresetService presetService;
        private Preset selectedPreset;
        private IErrorService errorService;

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HandBrakeWPF.ViewModels.PresetsViewModel"/> class.
        /// </summary>
        /// <param name="windowManager">
        /// The window Manager.
        /// </param>
        public PresetsViewModel(IWindowManager windowManager, IPresetService presetService, IErrorService errorService)
        {
            this.errorService = errorService;
            this.presetService = presetService;
            this.windowManager = windowManager;
            //this.PresetsDefaultsViewModel = new PresetsDefaultsViewModel();
            this.Presets = new BindingList<IPresetObject>();
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets or sets Presets.
        /// </summary>
        public IEnumerable<IPresetObject> Presets { get; set; }
        public EncodeTask CurrentTask { get; set; }
        public Title SelectedTitle { get; set; }
        public AudioBehaviours AudioBehaviours { get; set; }
        public SubtitleBehaviours SubtitleBehaviours { get; set; }
        public object SelectedPreset { get; set; }



        /// <summary>
        /// Gets or sets the preset defaults view model.
        /// </summary>
        //public IPresetsDefaultsViewModel PresetsDefaultsViewModel { get; set; }

        /// <summary>
        /// Add a Preset
        /// </summary>
        public void PresetAdd()
        {
            // TODO select the new preset.
            IAddPresetViewModel presetViewModel = IoC.Get<IAddPresetViewModel>();
            presetViewModel.Setup(this.CurrentTask, this.SelectedTitle, this.AudioBehaviours, this.SubtitleBehaviours);
            this.windowManager.ShowDialog(presetViewModel);
            this.NotifyOfPropertyChange(() => this.Presets);
        }

        /// <summary>
        /// Update a selected preset.
        /// </summary>
        public void PresetUpdate()
        {
            if (this.SelectedPreset == null)
            {
                this.errorService.ShowMessageBox(
                    Resources.Main_SelectPresetForUpdate, Resources.Main_NoPresetSelected, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (this.selectedPreset.IsBuildIn)
            {
                this.errorService.ShowMessageBox(
                    Resources.Main_NoUpdateOfBuiltInPresets, Resources.Main_NoPresetSelected, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (this.errorService.ShowMessageBox(Resources.Main_PresetUpdateConfirmation, Resources.AreYouSure, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                this.selectedPreset.Update(new EncodeTask(this.CurrentTask), new AudioBehaviours(this.AudioBehaviours), new SubtitleBehaviours(this.SubtitleBehaviours));
                this.presetService.Update(this.selectedPreset);

                this.errorService.ShowMessageBox(
                        Resources.Main_PresetUpdated, Resources.Updated, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Manage the current Preset
        /// </summary>
        public void PresetManage()
        {
            if (this.SelectedPreset == null)
            {
                this.errorService.ShowMessageBox(
                    Resources.Main_SelectPresetForUpdate, Resources.Main_NoPresetSelected, MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (this.selectedPreset.IsBuildIn)
            {
                this.errorService.ShowMessageBox(
                    Resources.Main_NoUpdateOfBuiltInPresets, Resources.Main_NoPresetSelected, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IManagePresetViewModel presetViewModel = IoC.Get<IManagePresetViewModel>();
            presetViewModel.Setup(this.selectedPreset);
            this.windowManager.ShowDialog(presetViewModel);
            Preset preset = presetViewModel.Preset;

            this.SelectedPreset = preset; // Reselect the preset
        }

        /// <summary>
        /// Remove a Preset
        /// </summary>
        public void PresetRemove()
        {
            if (this.selectedPreset != null)
            {
                if (this.selectedPreset.IsDefault)
                {
                    this.errorService.ShowMessageBox(
                      Resources.MainViewModel_CanNotDeleteDefaultPreset,
                      Resources.Warning,
                      MessageBoxButton.OK,
                      MessageBoxImage.Information);

                    return;
                }

                MessageBoxResult result =
                this.errorService.ShowMessageBox(
                   Resources.MainViewModel_PresetRemove_AreYouSure + this.selectedPreset.Name + " ?",
                   Resources.Question,
                   MessageBoxButton.YesNo,
                   MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                this.presetService.Remove(this.selectedPreset);
                this.NotifyOfPropertyChange(() => this.Presets);
            }
            else
            {
                MessageBox.Show(Resources.Main_SelectPreset, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Set a default preset
        /// </summary>
        public void PresetSetDefault()
        {
            if (this.selectedPreset != null)
            {
                this.presetService.SetDefault(this.selectedPreset);
                MessageBox.Show(string.Format(Resources.Main_NewDefaultPreset, this.selectedPreset.Name), Resources.Main_Presets, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(Resources.Main_SelectPreset, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Import a Preset
        /// </summary>
        public void PresetImport()
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Preset Files|*.json;*.plist", CheckFileExists = true };
            bool? dialogResult = dialog.ShowDialog();
            if (dialogResult.HasValue && dialogResult.Value)
            {
                this.presetService.Import(dialog.FileName);
                this.NotifyOfPropertyChange(() => this.Presets);
            }
        }

        /// <summary>
        /// Export a Preset
        /// </summary>
        public void PresetExport()
        {
            if (this.selectedPreset != null && !this.selectedPreset.IsBuildIn)
            {
                SaveFileDialog savefiledialog = new SaveFileDialog
                {
                    Filter = "json|*.json",
                    CheckPathExists = true,
                    AddExtension = true,
                    DefaultExt = ".json",
                    OverwritePrompt = true,
                    FilterIndex = 0
                };

                savefiledialog.ShowDialog();
                string filename = savefiledialog.FileName;

                if (!string.IsNullOrEmpty(filename))
                {
                    this.presetService.Export(savefiledialog.FileName, this.selectedPreset, HBConfigurationFactory.Create());
                }
            }
            else
            {
                MessageBox.Show(Resources.Main_SelectPreset, Resources.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Reset built-in presets
        /// </summary>
        public void PresetReset()
        {
            this.presetService.UpdateBuiltInPresets();
            this.NotifyOfPropertyChange(() => this.Presets);
            this.SelectedPreset = this.presetService.DefaultPreset;
            this.errorService.ShowMessageBox(Resources.Presets_ResetComplete, Resources.Presets_ResetHeader, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// The preset select.
        /// </summary>
        /// <param name="tag">
        /// The tag.
        /// </param>
        public void PresetSelect(object tag)
        {
            Preset preset = tag as Preset;
            if (preset != null)
            {
                this.SelectedPreset = preset;
            }
        }
        #endregion
    }
}
