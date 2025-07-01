using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using CliWrap;
using CliWrap.Buffered;

namespace TekkenFrameData.Library.Exstensions;

public static class ShellExstension
{
    public static async Task<string> Bash(this string cmd)
    {
        var escapedArgs = cmd.Replace("\"", "\\\"");

        var splits = escapedArgs.Split(' ');

        var command = splits.First();
        var args = splits.Skip(1).ToArray();

        var sb = new MemoryStream();
        var encoding = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Encoding.GetEncoding(866)
            : Encoding.UTF8;

        await CliWrap
            .Cli.Wrap(command)
            .WithArguments(args)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStream(sb))
            .WithStandardErrorPipe(PipeTarget.ToStream(sb))
            .ExecuteBufferedAsync(encoding, encoding);

        var text = Encoding.Convert(encoding, Encoding.UTF8, sb.GetBuffer());

        return Encoding.UTF8.GetString(text);
    }
}
