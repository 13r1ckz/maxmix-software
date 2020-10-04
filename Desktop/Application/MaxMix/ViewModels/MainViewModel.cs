﻿using MaxMix.Framework;
using MaxMix.Framework.Mvvm;
using MaxMix.Services.Audio;
using MaxMix.Services.Communication;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Input;

namespace MaxMix.ViewModels
{
    /// <summary>
    /// Main application view model class to be used as data context.
    /// </summary>
    internal class MainViewModel : BaseViewModel
    {
        #region UI Bindings
        /// <summary>
        /// Holds a reference to an instance of a settings view model.
        /// </summary>
        public SettingsViewModel SettingsViewModel
        {
            get => _settingsViewModel;
            private set => SetProperty(ref _settingsViewModel, value);
        }

        /// <summary>
        /// Holds the current state of the application.
        /// </summary>
        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// Status of the connection to a maxmix device.
        /// </summary>
        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        /// <summary>
        /// Sets the active state of the application to true.
        /// </summary>
        private ICommand _activateCommand;
        public ICommand ActivateCommand
        {
            get
            {
                if (_activateCommand == null)
                    _activateCommand = new DelegateCommand(() => IsActive = true);
                return _activateCommand;
            }
        }

        /// <summary>
        /// Sets the active state of the application to false.
        /// </summary>
        private ICommand _deactivateCommand;
        public ICommand DeactivateCommand
        {
            get
            {
                if (_deactivateCommand == null)
                    _deactivateCommand = new DelegateCommand(() => IsActive = false);
                return _deactivateCommand;
            }
        }

        /// <summary>
        /// Requests the shutdown process and notifies others by raising the ExitRequested event.
        /// </summary>
        private ICommand _requestExitCommand;
        public ICommand RequestExitCommand
        {
            get
            {
                if (_requestExitCommand == null)
                    _requestExitCommand = new DelegateCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));
                return _requestExitCommand;
            }
        }
        #endregion

        // Device State Tracking
        SessionInfo m_SessionInfo = SessionInfo.Default();
        SessionData[] m_Sessions = new SessionData[(int)SessionIndex.INDEX_MAX] { SessionData.Default(), SessionData.Default(), SessionData.Default(), SessionData.Default() };
        Dictionary<int, int> m_IndexToId = new Dictionary<int, int>();

        private IAudioSessionService _audioSessionService;
        private CommunicationService _communicationService;
        private SettingsViewModel _settingsViewModel;

        public MainViewModel()
        {
            _settingsViewModel = new SettingsViewModel();
            _settingsViewModel.PropertyChanged += OnSettingsChanged;

            _audioSessionService = new AudioSessionService();
            _audioSessionService.DefaultDeviceChanged += OnDefaultDeviceChanged;
            _audioSessionService.DeviceCreated += OnDeviceCreated;
            _audioSessionService.DeviceRemoved += OnDeviceRemoved;
            _audioSessionService.DeviceVolumeChanged += OnDeviceVolumeChanged;
            _audioSessionService.SessionCreated += OnAudioSessionCreated;
            _audioSessionService.SessionRemoved += OnAudioSessionRemoved;
            _audioSessionService.SessionVolumeChanged += OnAudioSessionVolumeChanged;

            _communicationService = new CommunicationService();
            _communicationService.OnMessageRecieved += OnMessageRecieved;
            _communicationService.OnDeviceConnected += OnDeviceConnected;
            _communicationService.OnDeviceDisconnected += OnDeviceDisconnected;
            _communicationService.OnFirmwareIncompatible += OnFirmwareIncompatible;
        }

        /// <summary>
        /// Raised to indicate the the shutdown of the application has been requested.
        /// </summary>
        public event EventHandler ExitRequested;

        public override void Start()
        {
            _communicationService.Start();
            _audioSessionService.Start();
            _settingsViewModel.Start();
        }

        public override void Stop()
        {
            _communicationService.Stop();
            _audioSessionService.Stop();
            _settingsViewModel.Stop();
        }

        private void OnDefaultDeviceChanged(object sender, int id, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            for (int i = (int)SessionIndex.INDEX_PREVIOUS; i < (int)SessionIndex.INDEX_MAX; i++)
            {
                if (m_Sessions[i].data.id == id)
                {
                    m_Sessions[i].data.isDefault = true;
                    _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                }
                else if (m_Sessions[i].data.isDefault)
                {
                    m_Sessions[i].data.isDefault = false;
                    _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                }
            }
        }

        private void OnDeviceCreated(object sender, int id, string displayName, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            // TODO: impl
        }

        private void OnDeviceRemoved(object sender, int id, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            // TODO: impl
        }

        private void OnDeviceVolumeChanged(object sender, int id, int volume, bool isMuted, DeviceFlow deviceFlow)
        {
            if (!IsConnected)
                return;

            UpdateSessionState(id, false, volume, isMuted);
        }

        private void OnAudioSessionCreated(object sender, int id, string displayName, int volume, bool isMuted)
        {
            if (!IsConnected)
                return;

            // TODO: impl
        }

        private void OnAudioSessionRemoved(object sender, int id)
        {
            if (!IsConnected)
                return;

            // TODO: impl
        }

        private void OnAudioSessionVolumeChanged(object sender, int id, int volume, bool isMuted)
        {
            if (!IsConnected)
                return;

            UpdateSessionState(id, false, volume, isMuted);
        }

        private void UpdateSessionState(int id, bool isDefault, int volume, bool isMuted, string name = null)
        {
            for (int i = (int)SessionIndex.INDEX_PREVIOUS; i < (int)SessionIndex.INDEX_MAX; i++)
            {
                if (m_Sessions[i].data.id == id)
                {
                    m_Sessions[i].data.isDefault = isDefault;
                    m_Sessions[i].data.volume = (byte)volume;
                    m_Sessions[i].data.isMuted = isMuted;
                    if (string.IsNullOrEmpty(name))
                        _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                    else
                    {
                        string prevName = m_Sessions[i].name;
                        m_Sessions[i].name = name;
                        if (m_Sessions[i].name != prevName)
                            _communicationService.SendMessage(Command.CURRENT_SESSION + i, m_Sessions[i]);
                        else
                            _communicationService.SendMessage(Command.VOLUME_CURR_CHANGE + i, m_Sessions[i]);
                    }
                    break;
                }
            }
        }

        private void OnSettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!IsConnected)
                return;

            DeviceSettings settings = _settingsViewModel.ToDeviceSettings();
            _communicationService.SendMessage(Command.SETTINGS, settings);
        }

        /****************************************
         * Communication Events
         ****************************************/
        private void OnFirmwareIncompatible(string obj)
        {
            // TODO: Display msg to user that firmware needs to be updated
        }

        private void OnDeviceDisconnected()
        {
            IsConnected = false;
            // Reset our tracked device state
        }

        private void OnDeviceConnected()
        {
            IsConnected = true;
            OnSettingsChanged(null, null);
            // Send device initial screen data

            // NOTE: we can have a setting to determin which initial screen we flip to
            IAudioDevice[] outputs = _audioSessionService.GetAudioDevices(DeviceFlow.Output);
            PopulateIndexToIdMap(outputs);
            int index = Array.FindIndex(outputs, x => x.IsDefault);
            int prevIndex = (index - 1 + outputs.Length) % outputs.Length;
            int nextIndex = (index + 1) % outputs.Length;

            m_SessionInfo.mode = DisplayMode.MODE_OUTPUT;
            m_SessionInfo.current = (byte)index;
            m_SessionInfo.output = (byte)outputs.Length;
            m_SessionInfo.input = (byte)_audioSessionService.GetAudioDevices(DeviceFlow.Input).Length;
            m_SessionInfo.output = (byte)_audioSessionService.GetAudioSessions().Length;

            m_Sessions[(int)SessionIndex.INDEX_CURRENT] = outputs[index].ToSessionData(index);
            m_Sessions[(int)SessionIndex.INDEX_NEXT] = outputs[nextIndex].ToSessionData(nextIndex);
            m_Sessions[(int)SessionIndex.INDEX_PREVIOUS] = outputs[prevIndex].ToSessionData(prevIndex);

            _communicationService.SendMessage(Command.CURRENT_SESSION, m_Sessions[(int)SessionIndex.INDEX_CURRENT]);
            _communicationService.SendMessage(Command.NEXT_SESSION, m_Sessions[(int)SessionIndex.INDEX_NEXT]);
            _communicationService.SendMessage(Command.PREVIOUS_SESSION, m_Sessions[(int)SessionIndex.INDEX_PREVIOUS]);
            _communicationService.SendMessage(Command.SESSION_INFO, m_SessionInfo);
        }

        private void OnMessageRecieved(Command command, IMessage message)
        {
            if (command == Command.VOLUME_CURR_CHANGE)
            {
                VolumeData vol = (VolumeData)message;
                _audioSessionService.SetItemVolume(m_IndexToId[vol.id], vol.volume, vol.isMuted);
            }
        }

        void PopulateIndexToIdMap(IAudioDevice[] devices)
        {
            m_IndexToId.Clear();
            for (int i = 0; i < devices.Length; i++)
                m_IndexToId[i] = devices[i].Id;
        }

        void PopulateIndexToIdMap(IAudioSession[] sessions)
        {
            m_IndexToId.Clear();
            for (int i = 0; i < sessions.Length; i++)
                m_IndexToId[i] = sessions[i].Id;
        }
    }
}
 