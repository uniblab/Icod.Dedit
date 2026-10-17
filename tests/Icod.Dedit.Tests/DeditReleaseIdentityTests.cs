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
	public void CandidateUsesPublishedWidgets170() {
		XDocument project = XDocument.Load( Path.Combine( RepositoryRoot(), "Icod.Dedit.csproj" ) );
		XElement dependency = Assert.Single( project.Descendants( "PackageReference" ),
			candidate => string.Equals( (string?)candidate.Attribute( "Include" ), "Icod.Widgets", StringComparison.Ordinal ) );

		Assert.Equal( "1.7.0", (string?)dependency.Attribute( "Version" ) );
		Assert.Equal( "1.0.0", Assert.Single( project.Descendants( "Version" ) ).Value );
		Assert.Equal( "1.0.0", Assert.Single( project.Descendants( "PackageVersion" ) ).Value );
	}

	[Fact]
	public void ToolVerifierRequiresWidgets170() {
		string verifier = File.ReadAllText( Path.Combine( RepositoryRoot(), "packaging", "VerifyDeditTool.ps1" ) );

		Assert.Contains( "Icod.Widgets/1.7.0", verifier, StringComparison.Ordinal );
		Assert.DoesNotContain( "Icod.Widgets/1.6.", verifier, StringComparison.Ordinal );
	}

	private static string RepositoryRoot() {
		DirectoryInfo? directory = new( AppContext.BaseDirectory );
		while ( directory is not null && !File.Exists( Path.Combine( directory.FullName, "Icod.Dedit.sln" ) ) ) directory = directory.Parent;
		return Assert.IsType<DirectoryInfo>( directory ).FullName;
	}
}
