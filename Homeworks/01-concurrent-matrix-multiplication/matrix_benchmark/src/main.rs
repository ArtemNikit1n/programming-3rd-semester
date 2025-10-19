use plotters::prelude::*;
use rand::Rng;
use serde::{Deserialize, Serialize};
use std::fs::{self, File, write};
use std::io::{BufWriter, Write};
use std::process::Command;
use std::time::Instant;

#[derive(Serialize, Deserialize, Debug)]
struct BenchmarkResult {
    matrix_size: String,
    sequential_time: f64,
    parallel_time: f64,
    speedup: f64,
    sequential_std_dev: f64,
    parallel_std_dev: f64,
    iterations: usize,
}

#[derive(Serialize, Deserialize, Debug)]
struct BenchmarkConfig {
    min_size: usize,
    max_size: usize,
    step: usize,
    iterations: usize,
    warmup_runs: usize,
}

fn generate_matrix(
    rows: usize,
    cols: usize,
    filename: &str,
) -> Result<(), Box<dyn std::error::Error>> {
    let file = File::create(filename)?;
    let mut writer = BufWriter::new(file);
    let mut rng = rand::thread_rng();

    for i in 0..rows {
        for j in 0..cols {
            write!(writer, "{}", rng.gen_range(0..100))?;
            if j < cols - 1 {
                write!(writer, " ")?;
            }
        }
        if i < rows - 1 {
            writeln!(writer)?;
        }
    }

    writer.flush()?;
    Ok(())
}

fn run_benchmark(
    rows: usize,
    cols: usize,
    common_dim: usize,
    iterations: usize,
) -> Result<(f64, f64, f64, f64), Box<dyn std::error::Error>> {
    generate_matrix(rows, common_dim, "matrix_a.txt")?;
    generate_matrix(common_dim, cols, "matrix_b.txt")?;

    let mut sequential_times = Vec::new();
    let mut parallel_times = Vec::new();

    let executable_path = "../MatrixBenchmark/bin/Release/net9.0/linux-x64/publish/MatrixBenchmark";

    if fs::metadata(executable_path).is_err() {
        return Err("C# executable not found. Please build it first with: dotnet publish -c Release -r linux-x64 --self-contained true".into());
    }

    for i in 0..iterations {
        println!("  Iteration {} of {iterations}", i + 1);

        let start = Instant::now();
        let output = Command::new(executable_path)
            .arg("matrix_a.txt")
            .arg("matrix_b.txt")
            .arg("result_seq.txt")
            .arg("--sequential")
            .output()?;

        if !output.status.success() {
            eprintln!(
                "Sequential multiplication failed: {}",
                String::from_utf8_lossy(&output.stderr)
            );
            continue;
        }
        sequential_times.push(start.elapsed().as_secs_f64());

        let start = Instant::now();
        let output = Command::new(executable_path)
            .arg("matrix_a.txt")
            .arg("matrix_b.txt")
            .arg("result_par.txt")
            .arg("--parallel")
            .output()?;

        if !output.status.success() {
            eprintln!(
                "Parallel multiplication failed: {}",
                String::from_utf8_lossy(&output.stderr)
            );
            continue;
        }
        parallel_times.push(start.elapsed().as_secs_f64());
    }

    if sequential_times.is_empty() || parallel_times.is_empty() {
        return Err("All benchmark iterations failed".into());
    }

    let seq_mean = mean(&sequential_times);
    let par_mean = mean(&parallel_times);
    let seq_std = standard_deviation(&sequential_times, seq_mean);
    let par_std = standard_deviation(&parallel_times, par_mean);

    Ok((seq_mean, par_mean, seq_std, par_std))
}

fn mean(data: &[f64]) -> f64 {
    data.iter().sum::<f64>() / data.len() as f64
}

fn standard_deviation(data: &[f64], mean: f64) -> f64 {
    let variance = data.iter().map(|&x| (x - mean).powi(2)).sum::<f64>() / data.len() as f64;
    variance.sqrt()
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let config = BenchmarkConfig {
        min_size: 100,
        max_size: 2000,
        step: 100,
        iterations: 5,
        warmup_runs: 2,
    };

    let mut results = Vec::new();

    println!("Running warmup...");
    for i in 0..config.warmup_runs {
        println!("Warmup run {} of {}", i + 1, config.warmup_runs);
        if let Err(e) = run_benchmark(100, 100, 100, 1) {
            eprintln!("Warmup failed: {e}");
        }
    }

    for size in (config.min_size..=config.max_size).step_by(config.step) {
        println!("Testing size {size}x{size}...");

        match run_benchmark(size, size, size, config.iterations) {
            Ok((seq_mean, par_mean, seq_std, par_std)) => {
                let speedup = seq_mean / par_mean;

                let result = BenchmarkResult {
                    matrix_size: format!("{size}x{size}"),
                    sequential_time: seq_mean,
                    parallel_time: par_mean,
                    speedup,
                    sequential_std_dev: seq_std,
                    parallel_std_dev: par_std,
                    iterations: config.iterations,
                };

                results.push(result);
                println!(
                    "Size {size}: Sequential: {seq_mean:.3}s ± {seq_std:.3}s, Parallel: {par_mean:.3}s ± {par_std:.3}s, Speedup: {speedup:.2}x"
                );
            }
            Err(e) => {
                eprintln!("Failed to benchmark size {size}: {e}");
                continue;
            }
        }
    }

    if results.is_empty() {
        return Err("No benchmark results were collected".into());
    }

    let json_results = serde_json::to_string_pretty(&results)?;
    write("benchmark_results.json", json_results)?;
    println!("Results saved to benchmark_results.json");

    if let Err(e) = generate_plot(&results) {
        eprintln!("Failed to generate plot: {e}");
    }

    let files_to_remove = [
        "matrix_a.txt",
        "matrix_b.txt",
        "result_seq.txt",
        "result_par.txt",
    ];
    for file in files_to_remove {
        if let Err(e) = fs::remove_file(file) {
            eprintln!("Warning: Failed to remove {file}: {e}");
        }
    }

    Ok(())
}

fn generate_plot(results: &[BenchmarkResult]) -> Result<(), Box<dyn std::error::Error>> {
    let root = BitMapBackend::new("benchmark_plot.png", (1024, 768)).into_drawing_area();
    root.fill(&WHITE)?;

    let max_time = results
        .iter()
        .map(|r| r.sequential_time.max(r.parallel_time))
        .fold(0.0, f64::max)
        * 1.1;

    let mut chart = ChartBuilder::on(&root)
        .caption("Matrix Multiplication Performance", ("sans-serif", 40))
        .margin(10)
        .x_label_area_size(40)
        .y_label_area_size(60)
        .build_cartesian_2d(0f64..results.len() as f64, 0f64..max_time)?;

    chart
        .configure_mesh()
        .x_desc("Matrix Size")
        .y_desc("Time (seconds)")
        .draw()?;

    chart
        .draw_series(LineSeries::new(
            results
                .iter()
                .enumerate()
                .map(|(i, r)| (i as f64, r.sequential_time)),
            &RED,
        ))?
        .label("Sequential")
        .legend(|(x, y)| PathElement::new(vec![(x, y), (x + 20, y)], RED));

    chart
        .draw_series(LineSeries::new(
            results
                .iter()
                .enumerate()
                .map(|(i, r)| (i as f64, r.parallel_time)),
            &BLUE,
        ))?
        .label("Parallel")
        .legend(|(x, y)| PathElement::new(vec![(x, y), (x + 20, y)], BLUE));

    chart
        .configure_series_labels()
        .background_style(WHITE.mix(0.8))
        .border_style(BLACK)
        .draw()?;

    let root2 = BitMapBackend::new("speedup_plot.png", (1024, 768)).into_drawing_area();
    root2.fill(&WHITE)?;

    let max_speedup = results.iter().map(|r| r.speedup).fold(0.0, f64::max) * 1.1;

    let mut chart2 = ChartBuilder::on(&root2)
        .caption("Speedup Factor", ("sans-serif", 40))
        .margin(10)
        .x_label_area_size(40)
        .y_label_area_size(60)
        .build_cartesian_2d(0f64..results.len() as f64, 0f64..max_speedup)?;

    chart2
        .configure_mesh()
        .x_desc("Matrix Size")
        .y_desc("Speedup (x)")
        .draw()?;

    chart2
        .draw_series(LineSeries::new(
            results
                .iter()
                .enumerate()
                .map(|(i, r)| (i as f64, r.speedup)),
            &GREEN,
        ))?
        .label("Speedup")
        .legend(|(x, y)| PathElement::new(vec![(x, y), (x + 20, y)], GREEN));

    chart2
        .configure_series_labels()
        .background_style(WHITE.mix(0.8))
        .border_style(BLACK)
        .draw()?;

    Ok(())
}
