# Chordality

**Chordality** is a high-performance, Windows Desktop Audio & DSP music theory engine and hardware-inspired MIDI controller. Built with modern C# (.NET 8) and Windows App SDK (WinUI 3), it serves as a dual-purpose educational visualizer and a master controller for external hardware synthesizers.

![Chordality Theme](https://img.shields.io/badge/Theme-Brutalist_Dark_Neon-050508?style=flat-square)
![Platform](https://img.shields.io/badge/Platform-Windows_10%2F11-blue?style=flat-square)
![Framework](https://img.shields.io/badge/Framework-.NET_8_%7C_WinUI_3-purple?style=flat-square)

## Overview

Chordality mathematically models advanced chord voicings, extensions, and inversions, providing **zero-latency audio feedback** and **sample-accurate sequencing** (Arpeggiator & Strum) using native NAudio WASAPI integration. It avoids standard UI threads and task delays, rendering sound directly on the audio thread for professional-grade timing.

## Core Features

*   **Music Theory Engine:** Pure functional math engine generating absolute MIDI notes (0-127) for complex voicings (e.g., `13#11`, `m7b5`) and dynamic inversions.
*   **Polymorphic Visualizers:** Switch instantly between Pad mode, active Note Blocks, a responsive 3-octave Virtual Piano, and an algorithmic Guitar Fretboard that calculates optimal human-playable fingerings (max 5-fret span).
*   **Circle of Fifths:** A fully interactive, touch-friendly radial UI calculated entirely via trigonometry to snap selections to relative major/minor nodes.
*   **Zero-Latency Audio Engine:** Built-in `PolyphonicSynthesizer` utilizing pure Sawtooth oscillators and ADSR envelopes to prevent clipping during rapid polyphony (up to 16 simultaneous voices).
*   **Sample-Accurate Sequencing:** Arp and Strum timings are calculated at the fractional sample level inside the NAudio buffer loop, guaranteeing zero thread-pool jitter.
*   **Hardware MIDI Controller:** Dynamically discover and connect to external plug-and-play synthesizers. Features a strict `ExternalMidiNoteTracker` to prevent infinite droning on hardware synths during rapid modulation.
*   **Lock-Free Stash Recorder:** A background concurrency-safe circular buffer (65,536 events) continuously records the last 20 minutes of your performance, allowing instant export to a Standard MIDI File (`.mid`) without blocking the audio thread.

## Repository Structure

The architecture enforces strict decoupling between state, visual rendering, audio processing, and hardware communication:

*   `Chordality.Engine/` - The pure C# functional music theory math engine (No UI or Audio dependencies).
*   `Chordality.Audio/` - The low-latency NAudio WASAPI DSP, Polyphonic Synth, and Sequencing engine.
*   `Chordality.App/` - The WinUI 3 front-end containing the XAML views, Windows MIDI Services, MVVM bindings, and hardware export logic.

## Documentation

For a deeper dive into the system design, please see the `docs/` folder:

*   [Architecture Overview](docs/ARCHITECTURE.md) - Deep dive into lock-free DSP threads and the Stash Buffer.
*   [How To Run](docs/HOW_TO_RUN.md) - Instructions for building, debugging, and packaging the MSIX bundle locally.