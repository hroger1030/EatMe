using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace EatMeClient
{
    /// <summary>
    /// Static owner of this frame's and the previous frame's raw input. Update() must be called
    /// exactly once per frame before anything reads it; the edge helpers (IsKeyPressed etc.) then
    /// report presses/releases relative to the prior frame.
    /// </summary>
    public static class InputManager
    {
        // This game client is a single player client.
        private const int MAX_INPUTS = 1;

        public enum MouseButton { Left, Right, Middle, XButton1, XButton2 }

        public static KeyboardState KeyboardState { get; private set; }
        public static KeyboardState PreviousKeyboardState { get; private set; }
        public static MouseState MouseState { get; private set; }
        public static MouseState PreviousMouseState { get; private set; }
        public static GamePadState[] GamePadState { get; private set; } = new GamePadState[MAX_INPUTS];
        public static GamePadState[] PreviousGamePadState { get; private set; } = new GamePadState[MAX_INPUTS];

        private static readonly List<char> _PendingCharacters = [];

        /// <summary>
        /// Characters typed since the last frame, in order, from the window's text input.
        /// Backspace arrives as ''.
        /// </summary>
        public static string TypedCharacters { get; private set; } = string.Empty;

        /// <summary>
        /// Hook for the window's TextInput event; picked up by the next Update.
        /// </summary>
        public static void AddTypedCharacter(char character)
        {
            _PendingCharacters.Add(character);
        }

        public static void Update()
        {
            PreviousKeyboardState = KeyboardState;
            PreviousMouseState = MouseState;
            Array.Copy(GamePadState, PreviousGamePadState, MAX_INPUTS);

            TypedCharacters = new string(_PendingCharacters.ToArray());
            _PendingCharacters.Clear();

            KeyboardState = Keyboard.GetState();
            MouseState = Mouse.GetState();

            for (int i = 0; i < MAX_INPUTS; i++)
                GamePadState[i] = GamePad.GetState((PlayerIndex)i);
        }

        private static ButtonState GetButtonState(MouseState state, MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => state.LeftButton,
                MouseButton.Right => state.RightButton,
                MouseButton.Middle => state.MiddleButton,
                MouseButton.XButton1 => state.XButton1,
                MouseButton.XButton2 => state.XButton2,
                _ => throw new ArgumentOutOfRangeException(nameof(button)),
            };
        }

        public static bool KeyPressed(Keys key) => KeyboardState.IsKeyDown(key) && !PreviousKeyboardState.IsKeyDown(key);
        public static bool KeyReleased(Keys key) => !KeyboardState.IsKeyDown(key) && PreviousKeyboardState.IsKeyDown(key);

        public static bool AnyKeyPressed()
        {
            foreach (var key in KeyboardState.GetPressedKeys())
            {
                if (!PreviousKeyboardState.IsKeyDown(key))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True if the cursor moved since last frame. Menus use this so a resting cursor doesn't
        /// override keyboard/gamepad selection every frame.
        /// </summary>
        public static bool MouseMoved() => MouseState.X != PreviousMouseState.X || MouseState.Y != PreviousMouseState.Y;

        public static bool MouseButtonPressed(MouseButton button) => GetButtonState(MouseState, button) == ButtonState.Pressed && GetButtonState(PreviousMouseState, button) == ButtonState.Released;
        public static bool MouseButtonReleased(MouseButton button) => GetButtonState(MouseState, button) == ButtonState.Released && GetButtonState(PreviousMouseState, button) == ButtonState.Pressed;

        public static bool GamePadButtonPressed(Buttons button, PlayerIndex player = PlayerIndex.One) => GamePadState[(int)player].IsButtonDown(button) && !PreviousGamePadState[(int)player].IsButtonDown(button);
        public static bool GamepadButtonReleased(Buttons button, PlayerIndex player = PlayerIndex.One) => !GamePadState[(int)player].IsButtonDown(button) && PreviousGamePadState[(int)player].IsButtonDown(button);
    }
}
