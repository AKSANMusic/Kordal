using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Chordality.App.Services.Midi;

public class MidiDeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public interface IMidiOutputService
{
    ObservableCollection<MidiDeviceInfo> OutputDevices { get; }

    Task ConnectAsync(string deviceId);
    void Disconnect();

    void SendNoteOn(byte channel, byte note, byte velocity);
    void SendNoteOff(byte channel, byte note);
    void SendAllNotesOff(byte channel);

    bool IsConnected { get; }
}
