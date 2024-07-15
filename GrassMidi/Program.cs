using System;
using NAudio.CoreAudioApi;
using NAudio.Midi;                              
class Program
{
    static void Main(string[] args)
    {

        int deviceCount = MidiIn.NumberOfDevices;
        for (int i = 0; i < deviceCount; i++)
        {
            var deviceInfo = MidiIn.DeviceInfo(i);
            Console.WriteLine($"设备序号: {i}, 设备名称: {deviceInfo.ProductName}");
        }
        for (int device = 0; device < MidiIn.NumberOfDevices; device++)
        {
            try
            {
                using (var midiIn = new MidiIn(device))
                {
                    Console.WriteLine($"设备 {device} 可以被访问");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设备 {device} 正在被占用: {ex.Message}");
            }
        }
        Console.WriteLine("请输入要监听的设备号");
        string str = Console.ReadLine();
        int Dnum;
        bool success = int.TryParse(str, out Dnum);
        if (success)
        {
            Console.WriteLine(Dnum);  // 输出：123
        }
        else
        {
            Console.WriteLine("字符串不能转换为整数");
        }

        using (var inputDevice = new MidiIn(Dnum)) // 0 表示第一个 MIDI 输入设备
        {
            inputDevice.MessageReceived += InputDevice_MessageReceived;
            inputDevice.Start();

            Console.WriteLine("按下除midi外任意键退出");
            Console.ReadKey(true);
        }
    }



    private static void InputDevice_MessageReceived(object sender, MidiInMessageEventArgs e)
    {
        if (e.MidiEvent.CommandCode == MidiCommandCode.NoteOn)
        {
            var noteOnEvent = (NoteOnEvent)e.MidiEvent;
            Console.WriteLine("-------------");
            Console.WriteLine($"键位值: {noteOnEvent.NoteNumber}");
            Console.WriteLine(noteOnEvent);
        }
        if (e.MidiEvent.CommandCode == MidiCommandCode.ControlChange)
        {
            var controlChangeEvent = (ControlChangeEvent)e.MidiEvent;
            Console.WriteLine("-------------");
            Console.WriteLine($"旋钮: {controlChangeEvent.Controller}, 值: {controlChangeEvent.ControllerValue}");
            Console.WriteLine(controlChangeEvent);
        }
        if (e.MidiEvent.CommandCode == MidiCommandCode.PitchWheelChange)
        {
            var pitchChangeEvent = (PitchWheelChangeEvent)e.MidiEvent;
            Console.WriteLine("-------------");
            Console.WriteLine($"Pitch: {pitchChangeEvent.Pitch}");
            Console.WriteLine(pitchChangeEvent);
        }
        switch (e.MidiEvent.CommandCode)
        {
            case MidiCommandCode.NoteOn:
                var noteOnEvent = (NoteOnEvent)e.MidiEvent;
                Console.WriteLine($"Note On: {noteOnEvent.NoteNumber}");
                break;


            case MidiCommandCode.ControlChange:
                var controlChangeEvent = (ControlChangeEvent)e.MidiEvent;
                Console.WriteLine($"Control Change: {controlChangeEvent.Controller}, Value: {controlChangeEvent.ControllerValue}");
                break;

            case MidiCommandCode.PitchWheelChange:
                var pitchWheelChangeEvent = (PitchWheelChangeEvent)e.MidiEvent;
                Console.WriteLine($"Pitch Wheel Change: {pitchWheelChangeEvent.Pitch}");
                break;

            case MidiCommandCode.ChannelAfterTouch:
                var channelAfterTouchEvent = (ChannelAfterTouchEvent)e.MidiEvent;
                Console.WriteLine($"Channel After Touch: {channelAfterTouchEvent.AfterTouchPressure}");
                break;

     
                // 其他MIDI消息类型...
        }
    }
}
