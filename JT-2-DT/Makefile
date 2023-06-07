benchmark-build: JT-2-DT.csproj
	dotnet build JT-2-DT.csproj -c Release
	rm -rf Benchmark_Build/
	mkdir Benchmark_Build/
	cp bin/Release/net6.0/JT* Benchmark_Build/
	cp -r Examples Benchmark_Build/
	cp -r external_executables Benchmark_Build/

small-benchmark: benchmark-build
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_1 1> results/2021_batch_1.csv 2> logs/2021_batch_1.log

benchmark-2021: benchmark-build 
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_1 1> results/2021_batch_1.csv 2> logs/2021_batch_1.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_2 1> results/2021_batch_2.csv 2> logs/2021_batch_2.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_3 1> results/2021_batch_3.csv 2> logs/2021_batch_3.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_4 1> results/2021_batch_4.csv 2> logs/2021_batch_4.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_5 1> results/2021_batch_5.csv 2> logs/2021_batch_5.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_6 1> results/2021_batch_6.csv 2> logs/2021_batch_6.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_7 1> results/2021_batch_7.csv 2> logs/2021_batch_7.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_8 1> results/2021_batch_8.csv 2> logs/2021_batch_8.log

benchmark-2022: benchmark-build
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_1 1> results/2022_batch_1.csv 2> logs/2022_batch_1.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_2 1> results/2022_batch_2.csv 2> logs/2022_batch_2.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_3 1> results/2022_batch_3.csv 2> logs/2022_batch_3.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_4 1> results/2022_batch_4.csv 2> logs/2022_batch_4.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_5 1> results/2022_batch_5.csv 2> logs/2022_batch_5.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_6 1> results/2022_batch_6.csv 2> logs/2022_batch_6.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_7 1> results/2022_batch_7.csv 2> logs/2022_batch_7.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_8 1> results/2022_batch_8.csv 2> logs/2022_batch_8.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_9 1> results/2022_batch_9.csv 2> logs/2022_batch_9.log

all-benchmark: benchmark-2021 benchmark-2022

baseline-2021: benchmark-build 
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_1 1> results/2021_baseline_batch_1.csv 2> logs/2021_baseline_batch_1.log
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_2 1> results/2021_baseline_batch_2.csv 2> logs/2021_baseline_batch_2.log
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_3 1> results/2021_baseline_batch_3.csv 2> logs/2021_baseline_batch_3.log
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_4 1> results/2021_baseline_batch_4.csv 2> logs/2021_baseline_batch_4.log
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_5 1> results/2021_baseline_batch_5.csv 2> logs/2021_baseline_batch_5.log
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_6 1> results/2021_baseline_batch_6.csv 2> logs/2021_baseline_batch_6.log
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_7 1> results/2021_baseline_batch_7.csv 2> logs/2021_baseline_batch_7.log
	-./Benchmark_Build/JT-2-DT --baseline 2021_batch_8 1> results/2021_baseline_batch_8.csv 2> logs/2021_baseline_batch_8.log

baseline-2022: benchmark-build
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_1 1> results/2022_baseline_batch_1.csv 2> logs/2022_baseline_batch_1.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_2 1> results/2022_baseline_batch_2.csv 2> logs/2022_baseline_batch_2.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_3 1> results/2022_baseline_batch_3.csv 2> logs/2022_baseline_batch_3.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_4 1> results/2022_baseline_batch_4.csv 2> logs/2022_baseline_batch_4.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_5 1> results/2022_baseline_batch_5.csv 2> logs/2022_baseline_batch_5.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_6 1> results/2022_baseline_batch_6.csv 2> logs/2022_baseline_batch_6.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_7 1> results/2022_baseline_batch_7.csv 2> logs/2022_baseline_batch_7.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_8 1> results/2022_baseline_batch_8.csv 2> logs/2022_baseline_batch_8.log
	-./Benchmark_Build/JT-2-DT --baseline 2022_batch_9 1> results/2022_baseline_batch_9.csv 2> logs/2022_baseline_batch_9.log

all-baseline: baseline-2021 baseline-2022
