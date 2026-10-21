/*
	Icod.Dedit.Tests
	Release identity tests for the dedit tool candidate.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Dedit.Tests;

using System.Xml.Linq;
using Xunit;

public sealed class DeditReleaseIdentityTests {
	[Fact]
	public void CandidateRetains100Identity() {
		XDocument project = XDocument.Load( Path.Combine( RepositoryRoot(), "Icod.Dedit.csproj" ) );

		Assert.Equal( "1.0.0", Assert.Single( project.Descendants( "Version" ) ).Value );
		Assert.Equal( "1.0.0", Assert.Single( project.Descendants( "PackageVersion" ) ).Value );
	}

	[Fact]
	public void CuratedNotesDescribeEditorScrollbarsAndDirtyCloseBehavior() {
		string notes = File.ReadAllText( Path.Combine( RepositoryRoot(), "docs", "releases", "1.0.0.md" ) );

		Assert.Contains( "auto-hidden vertical and horizontal scrollbars", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "no-wrap", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "◄/►", notes, StringComparison.Ordinal );
		Assert.Contains( "right-click", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "track clicks page", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "Ctrl+Up/Ctrl+Down", notes, StringComparison.Ordinal );
		Assert.Contains( "displayed rows", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "Yes / Don't Save / Cancel", notes, StringComparison.Ordinal );
		Assert.Contains( "Y/N/C", notes, StringComparison.Ordinal );
		Assert.Contains( "Cancel", notes, StringComparison.Ordinal );
		Assert.Contains( "red", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "dirty", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "compact", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "centered", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "identical text", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "mouse", notes, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "Ctrl+V", notes, StringComparison.Ordinal );
		Assert.Contains( "wrapped visual rows", notes, StringComparison.Ordinal );
		Assert.Contains( "preferred visual column", notes, StringComparison.Ordinal );
		Assert.Contains( "logical line and character", notes, StringComparison.Ordinal );
		Assert.Contains( "line numbers", notes, StringComparison.Ordinal );
		Assert.Contains( "themed current-line", notes, StringComparison.Ordinal );
		Assert.Contains( "decorative gutter clicks", notes, StringComparison.Ordinal );
		Assert.Contains( "wrapped continuation rows", notes, StringComparison.Ordinal );
		Assert.Contains( "narrow viewports suppress the gutter", notes, StringComparison.Ordinal );
	}

	[Fact]
	public void ProductSourceDoesNotUseObsoleteYesNoFactory() {
		string source = Path.Combine( RepositoryRoot(), "src" );
		foreach ( string path in Directory.EnumerateFiles( source, "*.cs", SearchOption.AllDirectories ) )
			Assert.DoesNotContain( "WidgetMessageDialog.YesNo(", File.ReadAllText( path ), StringComparison.Ordinal );
	}

	[Fact]
	public void LibraryContractsDescribeDialogOwnership() {
		string contracts = File.ReadAllText( Path.Combine( RepositoryRoot(), "docs", "Library-Contracts.md" ) );

		Assert.Contains( "`Arrange(CursesRectangle)`", contracts, StringComparison.Ordinal );
		Assert.Contains( "Dedit supplies the dirty-close dialog bounds", contracts, StringComparison.Ordinal );
	}

	[Fact]
	public void ReadmeConciseDescriptionIncludesNumberedEditor() {
		string readme = File.ReadAllText( Path.Combine( RepositoryRoot(), "README.md" ) );

		Assert.Contains( "Yes / Don't Save / Cancel", readme, StringComparison.Ordinal );
		Assert.Contains( "Y/N/C", readme, StringComparison.Ordinal );
		Assert.Contains( "Cancel selected by default", readme, StringComparison.Ordinal );
		Assert.Contains( "fixed line numbers in a decorative left gutter", readme, StringComparison.Ordinal );
		Assert.Contains( "current logical line", readme, StringComparison.Ordinal );
		Assert.Contains( "identical to an active selection", readme, StringComparison.Ordinal );
		Assert.Contains( "auto-hidden vertical and horizontal scrollbars", readme, StringComparison.OrdinalIgnoreCase );
		Assert.Contains( "no-wrap", readme, StringComparison.OrdinalIgnoreCase );
		Assert.DoesNotContain( "## Release history", readme, StringComparison.OrdinalIgnoreCase );
	}

	private static string RepositoryRoot() {
		DirectoryInfo? directory = new( AppContext.BaseDirectory );
		while ( directory is not null && !File.Exists( Path.Combine( directory.FullName, "Icod.Dedit.sln" ) ) ) directory = directory.Parent;
		return Assert.IsType<DirectoryInfo>( directory ).FullName;
	}
}
