using Godot;
using System;
public partial class MyAppEntry : Node2D
{
    private AudioEffectRecord _effect;
    private AudioStreamWav _recording;
    [Export] private Button PlayButton;
    [Export] private Button SaveButton;
    [Export] private Button RecordButton;
    [Export] private Label Status;
    [Export] private AudioStreamPlayer AudioStreamRecord;
    [Export] private AudioStreamPlayer AudioStreamPlayer;

    public override void _Ready()
    {
        PlayButton.Pressed += OnPlayButtonPressed;
        SaveButton.Pressed += OnSaveButtonPressed;
        RecordButton.Pressed += OnRecordButtonPressed;
        int idx = AudioServer.GetBusIndex("Record");
        _effect = (AudioEffectRecord)AudioServer.GetBusEffect(idx, 0);
    }

    private void OnRecordButtonPressed()
    {
        GD.Print("OnRecordButtonPressed");
    }

    private void OnPlayButtonPressed()
    {
        GD.Print("OnPlayButtonPressed");
    }

    private void OnSaveButtonPressed()
    {
        GD.Print("OnSaveButtonPressed");
    }
}