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
	await JsonSerializer.SerializeAsync(stream, new Test(123, "にほんご日本語"));
	Console.WriteLine(stream.GetString()); // {"Id":123,"Name":"\u306B\u307B\u3093\u3054\u65E5\u672C\u8A9E"} <- エスケープされてる
	stream.Seek(0, SeekOrigin.Begin);

	var doc = await JsonDocument.ParseAsync(stream);
	var prop = doc.RootElement.EnumerateObject().FirstOrDefault(x => x.NameEquals("Name")); // prop は JsonProperty 構造体
	Console.WriteLine(prop); // "Name":"\u306B\u307B\u3093\u3054\u65E5\u672C\u8A9E" <- 当然そのまま

	using var output = new MemoryStream();
	using var writer = new Utf8JsonWriter(output, new JsonWriterOptions() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) }); // Encoder 指定
	doc.WriteTo(writer); // この時点では writer で止まってる
	await writer.FlushAsync(); // ここで stream に書き込み

	Console.WriteLine(output.GetString()); // {"Id":123,"Name":"にほんご日本語"}
}
record Test(int Id, string Name);

static class StreamExtension
{
	public static string GetString(this MemoryStream stream) =>
		Encoding.UTF8.GetString(stream.ToArray());
}
