﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace MaxMix.Services.Audio
{
    /// <summary>
    /// Provides a facade with a simpler interface over multiple AudioSessions.
    /// </summary>
    public class AudioSessionGroup : IAudioSession
    {
        #region Constructor
        public AudioSessionGroup(int id, string displayName)
        {
            ID = id;
            DisplayName = displayName;
        }
        #endregion

        #region Events
        /// <inheritdoc/>
        public event Action<IAudioSession> VolumeChanged;

        /// <inheritdoc/>
        public event Action<IAudioSession> SessionEnded;
        #endregion

        #region Fields
        private readonly IDictionary<int, IAudioSession> _sessions = new ConcurrentDictionary<int, IAudioSession>();
        private int _volume = 100;
        private bool _isMuted = false;
        private bool _isNotifyEnabled = true;
        #endregion

        #region Properties
        /// <inheritdoc/>
        public int ID { get; protected set; }

        /// <inheritdoc/>
        public string DisplayName { get; protected set; }

        /// <inheritdoc/>
        public int Volume
        {
            get => _volume;
            set => SetVolume(value);
        }

        /// <inheritdoc/>
        public bool IsMuted
        {
            get => _isMuted;
            set => SetIsMuted(value);
        }
        #endregion

        #region Public Methods
        public void AddSession(IAudioSession session)
        {
            if (_sessions.ContainsKey(session.ID))
            {
                session.Dispose();
                return;
            }

            _sessions.Add(session.ID, session);
            session.VolumeChanged += OnVolumeChanged;
            session.SessionEnded += OnSessionEnded;

            if (_sessions.Count == 1)
            {
                _volume = session.Volume;
                _isMuted = session.IsMuted;

                VolumeChanged?.Invoke(this);
            }
        }
        #endregion

        #region Private Methods
        private void SetVolume(int value)
        {
            if (_volume == value)
                return;

            _isNotifyEnabled = false;
            _volume = value;
            foreach (var session in _sessions.Values)
                session.Volume = value;
        }

        private void SetIsMuted(bool value)
        {
            if (_isMuted == value)
                return;

            _isNotifyEnabled = false;
            _isMuted = value;
            foreach (var session in _sessions.Values)
                session.IsMuted = value;
        }
        #endregion

        #region Event Handlers
        private void OnVolumeChanged(IAudioSession session)
        {
            _volume = session.Volume;
            _isMuted = session.IsMuted;

            if (!_isNotifyEnabled)
            {
                _isNotifyEnabled = true;
                return;
            }

            VolumeChanged?.Invoke(this);
        }

        private void OnSessionEnded(IAudioSession session)
        {
            _sessions.Remove(session.ID);
            session.Dispose();

            if (_sessions.Count > 0)
                return;

            SessionEnded?.Invoke(this);
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            foreach (var session in _sessions.Values)
            {
                session.VolumeChanged -= OnVolumeChanged;
                session.SessionEnded -= OnSessionEnded;
                session.Dispose();
            }

            _sessions.Clear();
        }
        #endregion
    }
}
