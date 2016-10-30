# HotCommands

Project for creating new commands and shortcuts for Visual Studio.

<p><strong>Hot Commands for Visual Studio provides the follow features:</strong></p>
<table style="width: 600px;" border="1">
<tbody>
<tr>
<td style="text-align: left;"><strong>Feature</strong></td>
<td style="text-align: left;"><strong>Notes</strong></td>
<td style="text-align: left;"><strong>Shortcut</strong></td>
</tr>
<tr>
<td>Toggle Comment</td>
<td>Comments or Uncomments selected text or lines,<br /> or if no selection, Comments/Uncomments the current line then moves cursor down one line.</td>
<td>Ctrl+/</td>
</tr>
<tr>
<td>Duplicate Code /<br /> Duplicate Reversed</td>
<td>Duplicates the currently selected text, or the current line if no selection. <br /> Reversed: Same as Duplicate Code, but places the new code before the current selection (or line).</td>
<td>Ctrl+D /<br /> Ctrl+Shift+D</td>
</tr>
<tr>
<td>Format Code</td>
<td>Formats the selected text, or the whole document if no selection.</td>
<td>Ctrl+Alt+F</td>
</tr>
<tr>
<td>Increase Selection / Decrease Selection</td>
<td>Expands/Shrinks the current text selection by one level (ie. next largest/smallest code block level)</td>
<td>
<p>Ctrl+W /<br /> Ctrl+Shift+W&nbsp;</p>
</td>
</tr>
<tr>
<td>Go To Previous Member / <br /> Go To Next Member</td>
<td>Navigates to the previous/next member (ie. Method, Class, Field, Property)</td>
<td>Ctrl+Alt+UpArrow /<br /> Ctrl+Alt+DownArrow</td>
</tr>
<tr>
<td>Move Member Up /<br /> Move Member Down</td>
<td>Moves the current member above(/below) the previous(/next) member</td>
<td>
<p>Ctrl+Shift+Alt+UpArrow /<br /> Ctrl+Shift+Alt+DownArrow&nbsp;</p>
</td>
</tr>
<tr>
<td colspan="3"><strong>Refactoring Suggestions/Helpers</strong></td>
</tr>
<tr>
<td>Initialize Field From Constructor</td>
<td>Inserts variable as parameter to constructor and initializes it</td>
<td>
<p>Lightbulb action<br /> (Roslyn Analyzer)</p>
</td>
</tr>
<tr>
<td>Extract Class or Namespace</td>
<td>Extracts the selected class (or namespace) into a separate file</td>
<td>
<p>Lightbulb action<br /> (Roslyn Analyzer)</p>
</td>
</tr>
<tr>
<td>Change class modifier</td>
<td>Change class modifier to public, protected, internal, private, or protected internal</td>
<td>
<p>Lightbulb action<br /> (Roslyn Analyzer)</p>
</td>
</tr>
</tbody>
</table>

Notes:
Existings OOB commands defined in: 
VS\src\appid\cmddef\pkgui and 
VS\src\appid\inc
