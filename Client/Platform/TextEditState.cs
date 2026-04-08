using System;

namespace Client.Platform
{
    /// <summary>
    /// Cross-platform text editing state. Manages cursor position, selection, insert, delete,
    /// and password masking without any dependency on WinForms or DirectX text controls.
    /// </summary>
    public class TextEditState
    {
        private string _text = "";

        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? "";
                if (CursorPosition > _text.Length)
                    CursorPosition = _text.Length;
                ClearSelection();
            }
        }

        public int CursorPosition { get; private set; }
        public int SelectionStart { get; private set; }
        public int SelectionLength { get; private set; }
        public int MaxLength { get; set; } = int.MaxValue;
        public bool Password { get; set; }

        /// <summary>
        /// Returns true if there is an active selection.
        /// </summary>
        public bool HasSelection => SelectionLength != 0;

        /// <summary>
        /// The end index of the current selection.
        /// </summary>
        public int SelectionEnd => SelectionStart + SelectionLength;

        /// <summary>
        /// Inserts text at the current cursor position, replacing any active selection.
        /// Enforces MaxLength.
        /// </summary>
        public void InsertText(string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            if (HasSelection)
                DeleteSelection();

            int available = MaxLength - _text.Length;
            if (available <= 0) return;

            if (input.Length > available)
                input = input.Substring(0, available);

            _text = _text.Insert(CursorPosition, input);
            CursorPosition += input.Length;
        }

        /// <summary>
        /// Handles a key press with optional shift and ctrl modifiers.
        /// </summary>
        public void HandleKey(GameKeys key, bool shift, bool ctrl)
        {
            switch (key)
            {
                case GameKeys.Left:
                    if (ctrl)
                        MoveCursorWordLeft(shift);
                    else
                        MoveCursorLeft(shift);
                    break;

                case GameKeys.Right:
                    if (ctrl)
                        MoveCursorWordRight(shift);
                    else
                        MoveCursorRight(shift);
                    break;

                case GameKeys.Home:
                    MoveCursorToStart(shift);
                    break;

                case GameKeys.End:
                    MoveCursorToEnd(shift);
                    break;

                case GameKeys.Back:
                    if (ctrl)
                        BackspaceWord();
                    else
                        Backspace();
                    break;

                case GameKeys.Delete:
                    if (ctrl)
                        DeleteWord();
                    else
                        Delete();
                    break;

                case GameKeys.A:
                    if (ctrl)
                        SelectAll();
                    break;
            }
        }

        /// <summary>
        /// Returns the display text: the original text or asterisks if Password is set.
        /// </summary>
        public string GetDisplayText()
        {
            if (Password)
                return new string('*', _text.Length);
            return _text;
        }

        /// <summary>
        /// Returns the currently selected text, or an empty string if nothing is selected.
        /// </summary>
        public string GetSelectedText()
        {
            if (!HasSelection) return string.Empty;

            int start = Math.Min(SelectionStart, SelectionStart + SelectionLength);
            int length = Math.Abs(SelectionLength);

            if (start < 0) start = 0;
            if (start + length > _text.Length) length = _text.Length - start;

            return _text.Substring(start, length);
        }

        /// <summary>
        /// Selects all text and positions the cursor at the end.
        /// </summary>
        public void SelectAll()
        {
            if (_text.Length == 0) return;
            SelectionStart = 0;
            SelectionLength = _text.Length;
            CursorPosition = _text.Length;
        }

        /// <summary>
        /// Deletes the character after the cursor, or the selected text.
        /// </summary>
        public void Delete()
        {
            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition >= _text.Length) return;
            _text = _text.Remove(CursorPosition, 1);
        }

        /// <summary>
        /// Deletes the character before the cursor, or the selected text.
        /// </summary>
        public void Backspace()
        {
            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition <= 0) return;
            CursorPosition--;
            _text = _text.Remove(CursorPosition, 1);
        }

        /// <summary>
        /// Deletes the word before the cursor (Ctrl+Backspace behavior).
        /// </summary>
        public void BackspaceWord()
        {
            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition <= 0) return;

            int target = FindWordBoundaryLeft(CursorPosition);
            int count = CursorPosition - target;
            _text = _text.Remove(target, count);
            CursorPosition = target;
        }

        /// <summary>
        /// Deletes the word after the cursor (Ctrl+Delete behavior).
        /// </summary>
        public void DeleteWord()
        {
            if (HasSelection)
            {
                DeleteSelection();
                return;
            }

            if (CursorPosition >= _text.Length) return;

            int target = FindWordBoundaryRight(CursorPosition);
            int count = target - CursorPosition;
            _text = _text.Remove(CursorPosition, count);
        }

        /// <summary>
        /// Clears the current selection without deleting any text.
        /// </summary>
        public void ClearSelection()
        {
            SelectionStart = CursorPosition;
            SelectionLength = 0;
        }

        /// <summary>
        /// Sets the cursor to a specific position, optionally extending or clearing the selection.
        /// </summary>
        public void SetCursorPosition(int position, bool extendSelection = false)
        {
            position = Math.Clamp(position, 0, _text.Length);

            if (extendSelection)
            {
                // Anchor stays at SelectionStart if we already have a selection, otherwise at the old cursor pos
                if (!HasSelection)
                    SelectionStart = CursorPosition;

                CursorPosition = position;
                SelectionLength = CursorPosition - SelectionStart;
            }
            else
            {
                CursorPosition = position;
                ClearSelection();
            }
        }

        #region Private Helpers

        private void MoveCursorLeft(bool shift)
        {
            if (!shift && HasSelection)
            {
                // Collapse selection to the left edge
                int leftEdge = Math.Min(SelectionStart, SelectionEnd);
                CursorPosition = leftEdge;
                ClearSelection();
                return;
            }

            if (CursorPosition > 0)
                SetCursorPosition(CursorPosition - 1, shift);
        }

        private void MoveCursorRight(bool shift)
        {
            if (!shift && HasSelection)
            {
                // Collapse selection to the right edge
                int rightEdge = Math.Max(SelectionStart, SelectionEnd);
                CursorPosition = rightEdge;
                ClearSelection();
                return;
            }

            if (CursorPosition < _text.Length)
                SetCursorPosition(CursorPosition + 1, shift);
        }

        private void MoveCursorWordLeft(bool shift)
        {
            if (CursorPosition <= 0 && !HasSelection) return;

            int target = FindWordBoundaryLeft(CursorPosition);
            SetCursorPosition(target, shift);
        }

        private void MoveCursorWordRight(bool shift)
        {
            if (CursorPosition >= _text.Length && !HasSelection) return;

            int target = FindWordBoundaryRight(CursorPosition);
            SetCursorPosition(target, shift);
        }

        private void MoveCursorToStart(bool shift)
        {
            SetCursorPosition(0, shift);
        }

        private void MoveCursorToEnd(bool shift)
        {
            SetCursorPosition(_text.Length, shift);
        }

        private void DeleteSelection()
        {
            if (!HasSelection) return;

            int start = Math.Min(SelectionStart, SelectionEnd);
            int length = Math.Abs(SelectionLength);

            if (start < 0) start = 0;
            if (start + length > _text.Length) length = _text.Length - start;

            _text = _text.Remove(start, length);
            CursorPosition = start;
            ClearSelection();
        }

        private int FindWordBoundaryLeft(int position)
        {
            if (position <= 0) return 0;

            int i = position - 1;

            // Skip whitespace
            while (i > 0 && char.IsWhiteSpace(_text[i]))
                i--;

            // Skip word characters
            while (i > 0 && !char.IsWhiteSpace(_text[i - 1]))
                i--;

            return i;
        }

        private int FindWordBoundaryRight(int position)
        {
            if (position >= _text.Length) return _text.Length;

            int i = position;

            // Skip word characters
            while (i < _text.Length && !char.IsWhiteSpace(_text[i]))
                i++;

            // Skip whitespace
            while (i < _text.Length && char.IsWhiteSpace(_text[i]))
                i++;

            return i;
        }

        #endregion
    }
}
