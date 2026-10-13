/*
	Icod.Dedit
	Keeps a bounded process-local clipboard for editor commands.
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

namespace Icod.Dedit.Presentation;

using System.Text;
using Icod.Widgets;

internal sealed class DeditClipboard {
	private static readonly UTF8Encoding StrictUtf8 = new( false, true );
	private const int MaximumBytes = 65_536;
	private readonly WidgetHost host;
	private readonly Func<string, CancellationToken, ValueTask>? write;
	private readonly Func<TimeSpan, CancellationToken, ValueTask<byte[]>>? read;
	private static readonly TimeSpan TerminalTimeout = TimeSpan.FromMilliseconds( 250 );

	internal DeditClipboard( WidgetHost host, Func<string, CancellationToken, ValueTask>? write = null,
		Func<TimeSpan, CancellationToken, ValueTask<byte[]>>? read = null ) {
		this.host = host ?? throw new ArgumentNullException( nameof( host ) );
		this.write = write; this.read = read;
	}
	internal string? Text { get; private set; }

	internal void SetText( string text ) {
		ArgumentNullException.ThrowIfNull( text );
		if ( StrictUtf8.GetByteCount( text ) > MaximumBytes ) throw new ArgumentOutOfRangeException( nameof( text ) );
		this.Text = text;
	}

	internal bool Complete( WidgetClipboardRequest request ) {
		ArgumentNullException.ThrowIfNull( request );
		if ( request.Operation is WidgetClipboardOperation.Copy or WidgetClipboardOperation.Cut ) {
			if ( request.Text is null || StrictUtf8.GetByteCount( request.Text ) > MaximumBytes ) return this.Apply( request, WidgetClipboardStatus.TooLarge );
			if ( !this.Apply( request, WidgetClipboardStatus.Success ) ) return false;
			this.Text = request.Text;
			return true;
		}
		return this.Text is null ? this.Apply( request, WidgetClipboardStatus.Unsupported ) :
			this.Apply( request, WidgetClipboardStatus.Success, this.Text );
	}


	internal async ValueTask<bool> CompleteAsync( WidgetClipboardRequest request ) {
		ArgumentNullException.ThrowIfNull( request );
		if ( request.Operation is WidgetClipboardOperation.Copy or WidgetClipboardOperation.Cut ) {
			if ( !this.Complete( request ) ) return false;
			if ( this.write is not null && this.Text is { } value ) {
				using CancellationTokenSource timeout = new( TerminalTimeout );
				try { await this.write( value, timeout.Token ); }
				catch ( Exception exception ) when ( IsUnavailable( exception ) ) { }
			}
			return true;
		}
		if ( this.read is not null ) {
			using CancellationTokenSource timeout = new( TerminalTimeout );
			try {
				byte[] bytes = await this.read( TerminalTimeout, timeout.Token );
				if ( bytes.Length <= MaximumBytes ) {
					string value = StrictUtf8.GetString( bytes );
					bool applied = this.Apply( request, WidgetClipboardStatus.Success, value );
					if ( applied ) this.Text = value;
					return applied;
				}
			} catch ( Exception exception ) when ( IsUnavailable( exception ) ) { }
		}
		return this.Complete( request );
	}

	private static bool IsUnavailable( Exception exception ) => exception is TimeoutException or NotSupportedException
		or InvalidOperationException or FormatException or DecoderFallbackException or IOException
		or OperationCanceledException;

	private bool Apply( WidgetClipboardRequest request, WidgetClipboardStatus status, string? text = null ) {
		bool applied = this.host.ApplyClipboardResult( new WidgetClipboardResult( request.RequestId, status, text ) );
		_ = this.host.DrainActions();
		return applied && status == WidgetClipboardStatus.Success;
	}
}
