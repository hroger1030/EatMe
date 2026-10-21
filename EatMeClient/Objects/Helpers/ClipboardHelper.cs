using System;
using System.IO;
using System.Runtime.InteropServices;

namespace EatMeClient
{
    /// <summary>
    /// The system clipboard through SDL2, which MonoGame DesktopGL already loads from the exe's folder, so it
    /// works on Windows and macOS with no extra dependency. Any failure means an empty paste or a copy that does nothing.
    /// </summary>
    public static class ClipboardHelper
    {
        private static readonly string[] SDL_LIBRARY_NAMES = ["SDL2.dll", "libSDL2-2.0.0.dylib", "libSDL2-2.0.so.0"];

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetClipboardTextFunction();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SetClipboardTextFunction(IntPtr utf8Text);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void FreeFunction(IntPtr memory);

        private static bool _Loaded;
        private static GetClipboardTextFunction _GetClipboardText;
        private static SetClipboardTextFunction _SetClipboardText;
        private static FreeFunction _Free;

        private static bool Load()
        {
            if (_Loaded)
                return _GetClipboardText != null;

            _Loaded = true;

            try
            {
                foreach (var name in SDL_LIBRARY_NAMES)
                {
                    if (!NativeLibrary.TryLoad(Path.Combine(AppContext.BaseDirectory, name), out var library) && !NativeLibrary.TryLoad(name, out library))
                        continue;

                    if (NativeLibrary.TryGetExport(library, "SDL_GetClipboardText", out var getText) &&
                        NativeLibrary.TryGetExport(library, "SDL_SetClipboardText", out var setText) &&
                        NativeLibrary.TryGetExport(library, "SDL_free", out var free))
                    {
                        _GetClipboardText = Marshal.GetDelegateForFunctionPointer<GetClipboardTextFunction>(getText);
                        _SetClipboardText = Marshal.GetDelegateForFunctionPointer<SetClipboardTextFunction>(setText);
                        _Free = Marshal.GetDelegateForFunctionPointer<FreeFunction>(free);
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // no clipboard; paste stays empty
            }

            _GetClipboardText = null;
            return false;
        }

        /// <summary>
        /// The clipboard's text, or empty if there is none or it can't be read.
        /// </summary>
        public static string GetText()
        {
            if (!Load())
                return string.Empty;

            try
            {
                var text = _GetClipboardText();

                if (text == IntPtr.Zero)
                    return string.Empty;

                try
                {
                    return Marshal.PtrToStringUTF8(text) ?? string.Empty;
                }
                finally
                {
                    _Free(text);
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Puts text on the clipboard. Does nothing if the clipboard can't be reached.
        /// </summary>
        public static void SetText(string text)
        {
            if (!Load())
                return;

            var utf8Text = IntPtr.Zero;

            try
            {
                utf8Text = Marshal.StringToCoTaskMemUTF8(text ?? string.Empty);
                _SetClipboardText(utf8Text);
            }
            catch (Exception)
            {
                // copy does nothing
            }
            finally
            {
                if (utf8Text != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(utf8Text);
            }
        }
    }
}
