// <copyright file="Matrix.cs" company="ArtemNikit1n">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace ConcurrentMatrixMultiplication;

using System.Text;

/// <summary>
/// Class for concurrent multiplication of matrices loaded from a user file.
/// </summary>
public class Matrix
{
    private readonly int[,] data;

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class from a two-dimensional array.
    /// Creates a deep copy of the provided array data.
    /// </summary>
    /// <param name="data">The two-dimensional integer array containing matrix data.
    /// Must not be null and must have positive dimensions.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> has zero dimensions.</exception>
    public Matrix(int[,] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.GetLength(0) == 0 || data.GetLength(1) == 0)
        {
            throw new ArgumentException("Matrix dimensions must be positive.", nameof(data));
        }

        this.data = (int[,])data.Clone();
        this.NumberOfRows = data.GetLength(0);
        this.NumberOfColumns = data.GetLength(1);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class.
    /// Create zero matrix.
    /// </summary>
    /// <param name="numberOfRows">Rows in matrix.</param>
    /// <param name="numberOfColumns">Colum in matrix.</param>
    public Matrix(int numberOfRows, int numberOfColumns)
    {
        if (numberOfRows <= 0 || numberOfColumns <= 0)
        {
            throw new ArgumentOutOfRangeException($"{nameof(numberOfColumns)} and {nameof(numberOfRows)} must be positive.");
        }

        this.data = new int[numberOfRows, numberOfColumns];
        this.NumberOfColumns = numberOfColumns;
        this.NumberOfRows = numberOfRows;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class.
    /// </summary>
    /// <param name="filePath">Path to file with matrix.</param>
    public Matrix(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        var result = ReadFromFilePath(filePath);
        this.data = result.Data;
        this.NumberOfRows = result.Rows;
        this.NumberOfColumns = result.Columns;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class.
    /// </summary>
    /// <param name="fileStream">File stream with matrix data.</param>
    public Matrix(FileStream fileStream)
    {
        ArgumentNullException.ThrowIfNull(fileStream);
        var result = ReadFromFileStream(fileStream);
        this.data = result.Data;
        this.NumberOfRows = result.Rows;
        this.NumberOfColumns = result.Columns;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Matrix"/> class.
    /// </summary>
    /// <param name="stream">Stream with matrix data.</param>
    public Matrix(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var result = ReadFromStream(stream);
        this.data = result.Data;
        this.NumberOfRows = result.Rows;
        this.NumberOfColumns = result.Columns;
    }

    /// <summary>
    /// Gets number of rows.
    /// </summary>
    public int NumberOfRows { get; }

    /// <summary>
    /// Gets number of columns.
    /// </summary>
    public int NumberOfColumns { get; }

    /// <summary>
    /// Gets element at specified position.
    /// </summary>
    /// <param name="row">Row index.</param>
    /// <param name="column">Column index.</param>
    /// <returns>Element value.</returns>
    public int this[int row, int column]
    {
        get
        {
            if (row < 0 || row >= this.NumberOfRows)
            {
                throw new ArgumentOutOfRangeException(nameof(row), $"Row index must be between 0 and {this.NumberOfRows - 1}");
            }

            if (column < 0 || column >= this.NumberOfColumns)
            {
                throw new ArgumentOutOfRangeException(nameof(column), $"Column index must be between 0 and {this.NumberOfColumns - 1}");
            }

            return this.data[row, column];
        }
    }

    /// <summary>
    /// Multiplies two matrices sequentially without parallelization.
    /// </summary>
    /// <param name="leftMatrix">Left matrix.</param>
    /// <param name="rightMatrix">Right matrix.</param>
    /// <returns>The result of multiplication.</returns>
    public static Matrix MultiplySequential(Matrix leftMatrix, Matrix rightMatrix)
    {
        ArgumentNullException.ThrowIfNull(leftMatrix);
        ArgumentNullException.ThrowIfNull(rightMatrix);

        if (leftMatrix.NumberOfColumns != rightMatrix.NumberOfRows)
        {
            throw new ArgumentException("Matrices cannot be multiplied.");
        }

        Matrix resultMatrix = new(leftMatrix.NumberOfRows, rightMatrix.NumberOfColumns);
        MultiplyRowsRange(leftMatrix, rightMatrix, 0, leftMatrix.NumberOfRows, resultMatrix);
        return resultMatrix;
    }

    /// <summary>
    /// Multiplies two matrices from files and saves result to a third file.
    /// </summary>
    /// <param name="leftMatrixPath">Path to the left matrix file.</param>
    /// <param name="rightMatrixPath">Path to the right matrix file.</param>
    /// <param name="resultMatrixPath">Path to save the result matrix.</param>
    public static void Multiply(string leftMatrixPath, string rightMatrixPath, string resultMatrixPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(leftMatrixPath);
        ArgumentException.ThrowIfNullOrEmpty(rightMatrixPath);
        ArgumentException.ThrowIfNullOrEmpty(resultMatrixPath);

        Matrix leftMatrix = new(leftMatrixPath);
        Matrix rightMatrix = new(rightMatrixPath);
        var resultMatrix = Multiply(leftMatrix, rightMatrix);

        resultMatrix.SaveToFile(resultMatrixPath);
    }

    /// <summary>
    /// Multiplies two matrices from files and returns the result.
    /// </summary>
    /// <param name="leftMatrixPath">Path to the left matrix file.</param>
    /// <param name="rightMatrixPath">Path to the right matrix file.</param>
    /// <returns>The result of multiplication.</returns>
    public static Matrix MultiplyFromFiles(string leftMatrixPath, string rightMatrixPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(leftMatrixPath);
        ArgumentException.ThrowIfNullOrEmpty(rightMatrixPath);

        Matrix leftMatrix = new(leftMatrixPath);
        Matrix rightMatrix = new(rightMatrixPath);

        return Multiply(leftMatrix, rightMatrix);
    }

    /// <summary>
    /// Multiplies two matrices.
    /// </summary>
    /// <param name="leftMatrix">Matrix that is multiplied from the left.</param>
    /// <param name="rightMatrix">Matrix that is multiplied from the right.</param>
    /// <returns>The result of multiplication.</returns>
    public static Matrix Multiply(Matrix leftMatrix, Matrix rightMatrix)
    {
        ArgumentNullException.ThrowIfNull(leftMatrix);
        ArgumentNullException.ThrowIfNull(rightMatrix);
        if (leftMatrix.NumberOfColumns != rightMatrix.NumberOfRows)
        {
            throw new ArgumentException("Matrices cannot be multiplied.");
        }

        Matrix resultMatrix = new(leftMatrix.NumberOfRows, rightMatrix.NumberOfColumns);

        var totalRows = leftMatrix.NumberOfRows;
        var threadsCount = Environment.ProcessorCount;
        var rowsPerThread = totalRows / threadsCount;
        var remainingRows = totalRows % threadsCount;

        var currentStart = 0;
        Queue<Thread> threads = new(threadsCount);
        for (var i = 0; i < threadsCount; i++)
        {
            var rowsToProcess = rowsPerThread + (i < remainingRows ? 1 : 0);
            var startRow = currentStart;
            var endRow = currentStart + rowsToProcess;

            Thread thread = new(() => MultiplyRowsRange(leftMatrix, rightMatrix, startRow, endRow, resultMatrix));
            thread.Start();
            threads.Enqueue(thread);

            currentStart = endRow;
        }

        while (threads.Count != 0)
        {
            threads.Dequeue().Join();
        }

        return resultMatrix;
    }

    /// <summary>
    /// Saves the matrix to a file.
    /// </summary>
    /// <param name="filePath">Path to the output file.</param>
    public void SaveToFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        using var writer = new StreamWriter(filePath);
        for (var i = 0; i < this.NumberOfRows; i++)
        {
            var row = new StringBuilder();
            for (var j = 0; j < this.NumberOfColumns; j++)
            {
                row.Append(this[i, j]);
                if (j < this.NumberOfColumns - 1)
                {
                    row.Append(' ');
                }
            }

            writer.WriteLine(row.ToString());
        }
    }

    private static void MultiplyRowsRange(
        Matrix leftMatrix,
        Matrix rightMatrix,
        int startRow,
        int endRow,
        Matrix resultMatrix)
    {
        for (var i = startRow; i < endRow; i++)
        {
            for (var j = 0; j < rightMatrix.NumberOfColumns; j++)
            {
                var sum = 0;
                for (var k = 0; k < leftMatrix.NumberOfColumns; k++)
                {
                    sum += leftMatrix[i, k] * rightMatrix[k, j];
                }

                resultMatrix.data[i, j] = sum;
            }
        }
    }

    private static (int[,] Data, int Rows, int Columns) ReadFromFilePath(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        using var fileStream = File.OpenRead(filePath);
        return ReadFromFileStream(fileStream);
    }

    private static (int[,] Data, int Rows, int Columns) ReadFromFileStream(FileStream fileStream)
    {
        return ReadFromStream(fileStream);
    }

    private static (int[,] Data, int Rows, int Columns) ReadFromStream(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return ParseMatrixContent(content);
    }

    private static (int[,] Data, int Rows, int Columns) ParseMatrixContent(string content)
    {
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            throw new InvalidDataException("File is empty.");
        }

        var firstLineValues = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var columns = firstLineValues.Length;
        var rows = lines.Length;

        var data = new int[rows, columns];

        for (var i = 0; i < rows; i++)
        {
            var values = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (values.Length != columns)
            {
                throw new FormatException($"Mismatch in number of elements in row {i + 1}. Expected: {columns}, Actual: {values.Length}");
            }

            for (var j = 0; j < columns; j++)
            {
                if (!int.TryParse(values[j], out var value))
                {
                    throw new FormatException($"Invalid number format in row {i + 1}, column {j + 1}: '{values[j]}'");
                }

                data[i, j] = value;
            }
        }

        return (data, rows, columns);
    }
}