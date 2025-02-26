﻿using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Rendering;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using ICSharpCode.AvalonEdit;
using StackTraceExplorer.Helpers;
using System.Linq;

namespace StackTraceExplorer.Generators
{
    public class MemberLinkElementGenerator : VisualLineElementGenerator
    {
        // To use this class:
        // textEditor.TextArea.TextView.ElementGenerators.Add(new MemberLinkElementGenerator());

        private readonly StackTraceEditor _textEditor;
        public static readonly Regex MemberRegex = new Regex(@"([A-Za-z0-9<>_`]+\.)*((.ctor|[A-Za-z0-9<>_\[,\]])+\(.*?\))", RegexOptions.IgnoreCase);

        private string _fullMatchText;

        public MemberLinkElementGenerator(StackTraceEditor textEditor)
        {
            _textEditor = textEditor;
        }

        private Match FindMatch(int startOffset)
        {
            // fetch the end offset of the VisualLine being generated
            var endOffset = CurrentContext.VisualLine.LastDocumentLine.EndOffset;
            var document = CurrentContext.Document;
            var relevantText = document.GetText(startOffset, endOffset - startOffset);
            return MemberRegex.Match(relevantText);
        }

        /// Gets the first offset >= startOffset where the generator wants to construct
        /// an element.
        /// Return -1 to signal no interest.
        public override int GetFirstInterestedOffset(int startOffset)
        {
            var m = FindMatch(startOffset);        
            return m.Success ? startOffset + m.Index : -1;
        }

        /// Constructs an element at the specified offset.
        /// May return null if no element should be constructed.
        public override VisualLineElement ConstructElement(int offset)
        {
            var match = FindMatch(offset);
            // check whether there's a match exactly at offset
            if (!match.Success || match.Index != 0) return null;

            // The first match returns the full method definition
            if (string.IsNullOrEmpty(_fullMatchText))
            {
                _fullMatchText = match.Value;
            }

            var captures = match.Groups[1].Captures.Cast<Capture>().Select(c => c.Value).ToList();
            captures.Add(match.Groups[2].Value);

            var lineElement = new CustomLinkVisualLineText(
                new [] { _fullMatchText, captures.First() }, 
                CurrentContext.VisualLine,
                captures.First().TrimEnd('.').Length,
                ToBrush(EnvironmentColors.StartPageTextControlLinkSelectedColorKey), 
                ClickHelper.HandleMemberLinkClicked, 
                false,
                CurrentContext.Document,
                _textEditor
            );

            // If we have created elements for the entire definition, reset. 
            // So we can create elements for more definitions
            if (_fullMatchText.EndsWith(captures.First()))
            {
                _fullMatchText = null;
            }

            if (TraceHelper.ViewModel.IsClickedLine(lineElement))
            {
                lineElement.ForegroundBrush = ToBrush(EnvironmentColors.StatusBarNoSolutionColorKey);
            }

            return lineElement;
        }

        private static SolidColorBrush ToBrush(ThemeResourceKey key)
        {
            var color = VSColorTheme.GetThemedColor(key);
            return new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}