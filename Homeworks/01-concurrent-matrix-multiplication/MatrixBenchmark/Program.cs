// <copyright file="Program.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using ConcurrentMatrixMultiplication;

if (args.Length >= 3)
{
    var leftPath = args[0];
    var rightPath = args[1];
    var resultPath = args[2];

    var useSequential = args.Length > 3 && args[3] == "--sequential";

    try
    {
        Matrix left = new(leftPath);
        Matrix right = new(rightPath);

        var result = useSequential ? Matrix.MultiplySequential(left, right) : Matrix.Multiply(left, right);

        result.SaveToFile(resultPath);
        Console.WriteLine($"Multiplication completed. Result saved to {resultPath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}
else
{
    Console.WriteLine("Usage: dotnet run -- <left_matrix> <right_matrix> <result> [--sequential|--parallel]");
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run -- matrix1.txt matrix2.txt result.txt --parallel");
    Console.WriteLine("  dotnet run -- matrix1.txt matrix2.txt result.txt --sequential");
}