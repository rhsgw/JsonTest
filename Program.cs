// See https://aka.ms/new-console-template for more information
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

// 日本語出力できるようにしとく
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.GetEncoding(932);

using (var stream = new MemoryStream())
{
	await JsonSerializer.SerializeAsync(stream, new Test(123, "test name"));
	Console.WriteLine(stream.GetString()); // { "Id":123,"Name":"test name"}
}
using (var stream = new MemoryStream())
{
	await JsonSerializer.SerializeAsync(stream, new Test(234, "日本語"));
	await stream.WriteAsync(new byte[] { 0x0d, 0x0a });
	await JsonSerializer.SerializeAsync(stream, new Test(345, "日本語"),
		// Unicode範囲全部許容 (エスケープしない)
		// これを入れないと、許容されてない範囲の文字は \uxxxx みたいなエスケープが入る
		// イチイチ許容範囲を個別指定することもできるけど面倒
		new JsonSerializerOptions()
		{
			Encoder =JavaScriptEncoder.Create(UnicodeRanges.All),
		});
	Console.WriteLine(stream.GetString());
	// {"Id":234,"Name":"\u65E5\u672C\u8A9E"}
	// { "Id":345,"Name":"日本語"}
}
record Test(int Id, string Name);

static class StreamExtension
{
	public static string GetString(this MemoryStream stream) =>
		Encoding.UTF8.GetString(stream.ToArray());
}
