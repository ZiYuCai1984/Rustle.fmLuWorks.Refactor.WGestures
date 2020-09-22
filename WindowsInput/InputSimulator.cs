﻿using System;

namespace WindowsInput
{
    /// <summary>
    ///     Implements the <see cref="IInputSimulator" /> interface to simulate Keyboard and Mouse input
    ///     and provide the state of those input devices.
    /// </summary>
    public class InputSimulator : IInputSimulator
    {
        /// <summary>
        ///     The <see cref="IInputDeviceStateAdapter" /> instance to use for interpreting the state of the
        ///     input devices.
        /// </summary>
        private readonly IInputDeviceStateAdapter _inputDeviceState;

        /// <summary>
        ///     The <see cref="IKeyboardSimulator" /> instance to use for simulating keyboard input.
        /// </summary>
        private readonly IKeyboardSimulator _keyboardSimulator;

        /// <summary>
        ///     The <see cref="IMouseSimulator" /> instance to use for simulating mouse input.
        /// </summary>
        private readonly IMouseSimulator _mouseSimulator;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InputSimulator" /> class using the specified
        ///     <see cref="IKeyboardSimulator" />, <see cref="IMouseSimulator" /> and
        ///     <see cref="IInputDeviceStateAdapter" /> instances.
        /// </summary>
        /// <param name="keyboardSimulator">
        ///     The <see cref="IKeyboardSimulator" /> instance to use for
        ///     simulating keyboard input.
        /// </param>
        /// <param name="mouseSimulator">
        ///     The <see cref="IMouseSimulator" /> instance to use for simulating
        ///     mouse input.
        /// </param>
        /// <param name="inputDeviceStateAdapter">
        ///     The <see cref="IInputDeviceStateAdapter" /> instance to use
        ///     for interpreting the state of input devices.
        /// </param>
        public InputSimulator(
            IKeyboardSimulator keyboardSimulator, IMouseSimulator mouseSimulator,
            IInputDeviceStateAdapter inputDeviceStateAdapter)
        {
            _keyboardSimulator = keyboardSimulator;
            _mouseSimulator = mouseSimulator;
            _inputDeviceState = inputDeviceStateAdapter;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="InputSimulator" /> class using the default
        ///     <see cref="KeyboardSimulator" />, <see cref="MouseSimulator" /> and
        ///     <see cref="WindowsInputDeviceStateAdapter" /> instances.
        /// </summary>
        public InputSimulator()
        {
            _keyboardSimulator = new KeyboardSimulator(this);
            _mouseSimulator = new MouseSimulator(this);
            _inputDeviceState = new WindowsInputDeviceStateAdapter();
        }

        /// <summary>
        ///     Tag to mark events
        /// </summary>
        public IntPtr ExtraInfo { get; set; }

        /// <summary>
        ///     Gets the <see cref="IKeyboardSimulator" /> instance for simulating Keyboard input.
        /// </summary>
        /// <value>The <see cref="IKeyboardSimulator" /> instance.</value>
        public IKeyboardSimulator Keyboard => _keyboardSimulator;

        /// <summary>
        ///     Gets the <see cref="IMouseSimulator" /> instance for simulating Mouse input.
        /// </summary>
        /// <value>The <see cref="IMouseSimulator" /> instance.</value>
        public IMouseSimulator Mouse => _mouseSimulator;

        /// <summary>
        ///     Gets the <see cref="IInputDeviceStateAdapter" /> instance for determining the state of the
        ///     various input devices.
        /// </summary>
        /// <value>The <see cref="IInputDeviceStateAdapter" /> instance.</value>
        public IInputDeviceStateAdapter InputDeviceState => _inputDeviceState;
    }
}
