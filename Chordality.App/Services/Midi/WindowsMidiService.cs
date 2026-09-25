using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Midi;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Chordality.App.Services.Midi;

public partial class WindowsMidiService : ObservableObject, IMidiOutputService, IDisposable
{
    public ObservableCollection<MidiDeviceInfo> OutputDevices { get; } = new();

    private DeviceWatcher? _watcher;
    private IMidiOutPort? _currentPort;
    private readonly ExternalMidiNoteTracker _noteTracker = new();

    // High-priority dedicated dispatch queue to avoid blocking NAudio
    private readonly System.Collections.Concurrent.BlockingCollection<Action> _midiQueue = new();
    private readonly Thread _midiDispatchThread;

    [ObservableProperty]
    private bool _isConnected;

    public WindowsMidiService()
    {
        _midiDispatchThread = new Thread(ProcessMidiQueue)
        {
            Name = "MidiOutputDispatcher",
            IsBackground = true,
            Priority = ThreadPriority.Highest
        };
        _midiDispatchThread.Start();

        StartDeviceWatcher();
    }

    private void ProcessMidiQueue()
    {
        foreach (var action in _midiQueue.GetConsumingEnumerable())
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"MIDI Dispatch Exception: {ex.Message}");
            }
        }
    }

    private void StartDeviceWatcher()
    {
        string midiOutSelector = MidiOutPort.GetDeviceSelector();
        _watcher = DeviceInformation.CreateWatcher(midiOutSelector);

        // Watcher events fire on background threads. In WinUI3, we need to dispatch to UI thread to modify ObservableCollections bound to XAML.
        var dispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        _watcher.Added += (s, e) =>
        {
            dispatcherQueue?.TryEnqueue(() =>
            {
                var info = new MidiDeviceInfo { Id = e.Id, Name = e.Name };
                OutputDevices.Add(info);
            });
        };

        _watcher.Removed += (s, e) =>
        {
            dispatcherQueue?.TryEnqueue(() =>
            {
                for (int i = OutputDevices.Count - 1; i >= 0; i--)
                {
                    if (OutputDevices[i].Id == e.Id)
                    {
                        OutputDevices.RemoveAt(i);
                    }
                }
            });
        };

        _watcher.Start();
    }

    public async Task ConnectAsync(string deviceId)
    {
        try
        {
            var port = await MidiOutPort.FromIdAsync(deviceId);
            if (port != null)
            {
                Disconnect();
                _currentPort = port;
                _noteTracker.SetPort(_currentPort);
                IsConnected = true;
                Debug.WriteLine($"Connected to MIDI out: {deviceId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to connect to MIDI device: {ex.Message}");
            IsConnected = false;
        }
    }

    public void Disconnect()
    {
        if (_currentPort != null)
        {
            _noteTracker.SetPort(null); // This sends All Notes Off
            _currentPort.Dispose();
            _currentPort = null;
        }
        IsConnected = false;
    }

    public void SendNoteOn(byte channel, byte note, byte velocity)
    {
        _midiQueue.Add(() => _noteTracker.NoteOn(channel, note, velocity));
    }

    public void SendNoteOff(byte channel, byte note)
    {
        _midiQueue.Add(() => _noteTracker.NoteOff(channel, note));
    }

    public void SendAllNotesOff(byte channel)
    {
        _midiQueue.Add(() => _noteTracker.AllNotesOff(channel));
    }

    public void Dispose()
    {
        _midiQueue.CompleteAdding();
        Disconnect();
    }
}
