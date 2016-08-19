﻿using Dopamine.Common.Services.Equalizer;
using Dopamine.Common.Services.Playback;
using Dopamine.Core.Audio;
using Dopamine.Core.Base;
using Dopamine.Core.Settings;
using Dopamine.Core.Utils;
using Prism.Mvvm;
using Prism.Commands;
using System.Collections.ObjectModel;

namespace Dopamine.ControlsModule.ViewModels
{
    public class EqualizerControlViewModel : BindableBase
    {
        #region Variables
        private IPlaybackService playbackService;
        private IEqualizerService equalizerService;

        private ObservableCollection<EqualizerPreset> presets;
        private EqualizerPreset selectedPreset;
        private bool isEqualizerEnabled;
        #endregion

        #region Commands
        public DelegateCommand ResetCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        #endregion

        #region Properties
        public ObservableCollection<EqualizerPreset> Presets
        {
            get { return this.presets; }
            set
            {
                SetProperty<ObservableCollection<EqualizerPreset>>(ref this.presets, value);
            }
        }

        public EqualizerPreset SelectedPreset
        {
            get { return this.selectedPreset; }
            set
            {
                value.BandValueChanged -= SelectedPreset_BandValueChanged;
                if (value.Name == Defaults.ManualPresetName) value.DisplayName = ResourceUtils.GetStringResource("Language_Manual"); // Make sure the manual preset is translated
                SetProperty<EqualizerPreset>(ref this.selectedPreset, value);
                if (XmlSettingsClient.Instance.Get<string>("Equalizer", "SelectedPreset") != value.Name) XmlSettingsClient.Instance.Set<string>("Equalizer", "SelectedPreset", value.Name);
                this.playbackService.SwitchPreset(ref value); // Make sure that playbackService has a reference to the selected preset
                value.BandValueChanged += SelectedPreset_BandValueChanged;
            }
        }

        public bool IsEqualizerEnabled
        {
            get { return this.isEqualizerEnabled; }
            set
            {
                SetProperty<bool>(ref this.isEqualizerEnabled, value);
                XmlSettingsClient.Instance.Set<bool>("Equalizer", "IsEnabled", value);
                this.playbackService.SetEqualizerEnabledState(value);
            }
        }
        #endregion

        #region Construction
        public EqualizerControlViewModel(IPlaybackService playbackService, IEqualizerService equalizerService)
        {
            // Variables
            this.playbackService = playbackService;
            this.equalizerService = equalizerService;

            this.IsEqualizerEnabled = XmlSettingsClient.Instance.Get<bool>("Equalizer", "IsEnabled");

            // Commands
            this.ResetCommand = new DelegateCommand(() =>
            {
                if (this.SelectedPreset.Name == Defaults.ManualPresetName)
                {
                    this.SelectedPreset.Reset();
                }
                else
                {
                    this.SelectedPreset = new EqualizerPreset(Defaults.ManualPresetName, false);
                }
                
                this.SaveManualPresetToSettings();
            });

            this.SaveCommand = new DelegateCommand(() => { this.SavePresetToFile(); });

            // Initialize
            this.InitializeAsync();
        }
        #endregion

        #region Private
        private async void InitializeAsync()
        {
            ObservableCollection<EqualizerPreset> localEqualizerPresets = new ObservableCollection<EqualizerPreset>();

            foreach (EqualizerPreset preset in await this.equalizerService.GetPresetsAsync())
            {
                if (preset.Name == Defaults.ManualPresetName) preset.DisplayName = ResourceUtils.GetStringResource("Language_Manual"); // Make sure the manual preset is translated
                localEqualizerPresets.Add(preset);
            }

            this.Presets = localEqualizerPresets;

            this.SelectedPreset = await this.equalizerService.GetSelectedPresetAsync(); // Don't use the SelectedPreset setter directly, because that saves SelectedPreset to the settings file.
        }

        private void SaveManualPresetToSettings()
        {
            // When the value of a slider changes, default to the manual preset the next time.
            XmlSettingsClient.Instance.Set<string>("Equalizer", "SelectedPreset", Defaults.ManualPresetName);

            // Also store the values for the manual preset
            XmlSettingsClient.Instance.Set<string>("Equalizer", "ManualPreset", this.SelectedPreset.ToValueString());
        }

        private void SavePresetToFile()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = ResourceUtils.GetStringResource("Language_Save_Preset");
            dlg.DefaultExt = FileFormats.EQUALIZERPRESET;
            dlg.Filter = string.Concat(ResourceUtils.GetStringResource("Language_Presets"), " (", FileFormats.EQUALIZERPRESET, ")|*", FileFormats.EQUALIZERPRESET);

            if ((bool)dlg.ShowDialog())
            {
            //            try
            //            {
            //                ExportPlaylistsResult result = await this.collectionService.ExportPlaylistAsync(this.SelectedPlaylists[0], dlg.FileName, false);

            //                if (result == ExportPlaylistsResult.Error)
            //                {
            //                    this.dialogService.ShowNotification(
            //                            0xe711,
            //                            16,
            //                            ResourceUtils.GetStringResource("Language_Error"),
            //                            ResourceUtils.GetStringResource("Language_Error_Saving_Playlist"),
            //                            ResourceUtils.GetStringResource("Language_Ok"),
            //                            true,
            //                            ResourceUtils.GetStringResource("Language_Log_File"));
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                LogClient.Instance.Logger.Error("Exception: {0}", ex.Message);

            //                this.dialogService.ShowNotification(0xe711, 16, ResourceUtils.GetStringResource("Language_Error"), ResourceUtils.GetStringResource("Language_Error_Saving_Playlist"), ResourceUtils.GetStringResource("Language_Ok"), true, ResourceUtils.GetStringResource("Language_Log_File"));
            //            }
            }
        }
        #endregion

        #region Event Handlers
        private void SelectedPreset_BandValueChanged(int bandIndex, double newValue)
        {
            this.SaveManualPresetToSettings();
        }
        #endregion
    }
}