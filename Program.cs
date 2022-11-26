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
	// 状況によって値の型が変わる配列を Deserialize したい
	// (objectの配列としてJSONに Serialize => 本来の型を与えて Deserialize)
	// RPC的な使い方を想定

	var paramTypesArray = new[] { typeof(int), typeof(double), typeof(ulong), typeof(string), typeof(Test), typeof(Test2), typeof(Test3) };
	await JsonSerializer.SerializeAsync(stream, new Request(123, "sample_method", 0, 1d, 2ul, "日本語", new Test(89, "internal"), new Test2(321, 654), new Test3(789, 456)));
	Console.WriteLine(stream.GetString());
	stream.Seek(0, SeekOrigin.Begin);

	Console.WriteLine();

	// 書き出した JSON を JsonDocument に Parse する
	var request = await JsonDocument.ParseAsync(stream);
	var parameters = request.RootElement.EnumerateObject().FirstOrDefault(x => x.NameEquals("Parameters"));
	Console.WriteLine($"Parameter's ValueKind : {parameters.Value.ValueKind}");

	Console.WriteLine();
	Console.WriteLine("Parameter member's ValueKind");
	foreach (var n in parameters.Value.EnumerateArray())
		Console.WriteLine($"\t{n.ValueKind}");

	Console.WriteLine();
	Console.WriteLine("Deserialized");

	var method = request.RootElement.EnumerateObject().FirstOrDefault(x => x.NameEquals("Method")).Value.GetString();
	Console.WriteLine($"Method = {method}");

	foreach (var n in parameters.Value.EnumerateArray().Zip(paramTypesArray,(v,t) => v.Deserialize(t)))
		Console.WriteLine($"\t{n?.GetType()}, {n}");
}

/*
{"Id":123,"Method":"sample_method","Parameters":[0,1,2,"\u65E5\u672C\u8A9E",{"Id":89,"Name":"internal"},{"P1":321,"P2":654},{"P1":789,"P2":456}]}

Parameter's ValueKind : Array

Parameter member's ValueKind
        Number
        Number
        Number
        String
        Object
        Object
        Object

Deserialized
Method = sample_method
        System.Int32, 0
        System.Double, 1
        System.UInt64, 2
        System.String, 日本語
        Test, Test { Id = 89, Name = internal }
        Test2, P1:0,P2:0
        Test3, P1:789,P2:456

Test2 だけ Deserialize できてないのは readonly struct で、パラメータの書き換えができないから
(Test3 みたいに readonly record struct にしちゃえばできる)
ちなみに、日本語文字列はエスケープされた状態からちゃんと戻されて扱われてる
(この JsonDocument をどこかに書き出すときには Encoder の指定が必要になる)
*/

// record class
record Test(int Id, string Name);

// readonly struct : Deserialize 不可
readonly struct Test2
{
	public int P1 { get; }
	public int P2 { get; }
	public Test2(int p1, int p2) => (P1, P2) = (p1, p2);
	public override string ToString() => $"P1:{P1},P2:{P2}";
}

// readonly record struct
readonly record struct Test3(int P1, int P2)
{
	public override string ToString() => $"P1:{P1},P2:{P2}";
}

class Request
{
	public int Id { get; }
	public string Method { get; }

	public IReadOnlyCollection<object?> Parameters { get; }

	public Request(int id, string method, params object?[] parameters)
	{
		Id = id;
		Method = method;
		Parameters = parameters;
	}
}

static class StreamExtension
{
	public static string GetString(this MemoryStream stream) =>
		Encoding.UTF8.GetString(stream.ToArray());
}
