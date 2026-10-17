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
	public void CandidateUsesPublishedWidgets171() {
		XDocument project = XDocument.Load( Path.Combine( RepositoryRoot(), "Icod.Dedit.csproj" ) );
		XElement dependency = Assert.Single( project.Descendants( "PackageReference" ),
			candidate => string.Equals( (string?)candidate.Attribute( "Include" ), "Icod.Widgets", StringComparison.Ordinal ) );

		Assert.Equal( "1.7.1", (string?)dependency.Attribute( "Version" ) );
		Assert.Equal( "1.0.0", Assert.Single( project.Descendants( "Version" ) ).Value );
		Assert.Equal( "1.0.0", Assert.Single( project.Descendants( "PackageVersion" ) ).Value );
	}

	[Fact]
	public void ToolVerifierRequiresWidgets171() {
		string verifier = File.ReadAllText( Path.Combine( RepositoryRoot(), "packaging", "VerifyDeditTool.ps1" ) );

		Assert.Contains( "Icod.Widgets/1.7.1", verifier, StringComparison.Ordinal );
		Assert.DoesNotContain( "Icod.Widgets/1.7.0", verifier, StringComparison.Ordinal );
	}

	[Fact]
	public void CuratedNotesDescribeWrappedCaretCorrection() {
		string notes = File.ReadAllText( Path.Combine( RepositoryRoot(), "docs", "releases", "1.0.0.md" ) );

		Assert.Contains( "Icod.Widgets 1.7.1", notes, StringComparison.Ordinal );
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
	public void ReadmeConciseDescriptionIncludesNumberedEditor() {
		string readme = File.ReadAllText( Path.Combine( RepositoryRoot(), "README.md" ) );

		Assert.Contains( "fixed line numbers in a decorative left gutter", readme, StringComparison.Ordinal );
		Assert.Contains( "current logical line", readme, StringComparison.Ordinal );
		Assert.DoesNotContain( "## Release history", readme, StringComparison.OrdinalIgnoreCase );
	}

	private static string RepositoryRoot() {
		DirectoryInfo? directory = new( AppContext.BaseDirectory );
		while ( directory is not null && !File.Exists( Path.Combine( directory.FullName, "Icod.Dedit.sln" ) ) ) directory = directory.Parent;
		return Assert.IsType<DirectoryInfo>( directory ).FullName;
	}
}
