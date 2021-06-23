using System;
using System.Windows;
using System.Windows.Controls;

namespace SipgateFaxdrucker
{
    class CustomCombobox : ComboBox
    {
        private int _caretPosition;
        private TextBox _textBox;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            var element = GetTemplateChild("PART_EditableTextBox");
            if (element != null)
            {
                _textBox = (TextBox)element;
                _textBox.SelectionChanged += OnDropSelectionChanged;
            }
        }

        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);

            if (_textBox.SelectionLength > 0)
            {
                _caretPosition = _textBox.SelectionLength; // caretPosition must be set to TextBox's SelectionLength
                _textBox.CaretIndex = _caretPosition;
            }
            if (_textBox.SelectionLength == 0 && _textBox.CaretIndex != 0)
            {
                _caretPosition = _textBox.CaretIndex;
            }
        }

        private void OnDropSelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox txt = (TextBox)sender;

            if (IsDropDownOpen && txt.SelectionLength > 0)
            {
                _caretPosition = txt.SelectionLength; // caretPosition must be set to TextBox's SelectionLength
                txt.CaretIndex = _caretPosition;
            }
            if (txt.SelectionLength == 0 && txt.CaretIndex != 0)
            {
                _caretPosition = txt.CaretIndex;
            }
        }


    }
}
