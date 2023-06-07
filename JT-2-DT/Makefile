benchmark-build: JT-2-DT.csproj
	dotnet build JT-2-DT.csproj -c Release
	rm -rf Benchmark_Build/
	mkdir Benchmark_Build/
	cp bin/Release/net6.0/JT* Benchmark_Build/
	cp -r Examples Benchmark_Build/
	cp -r external_executables Benchmark_Build/

benchmark-2021: benchmark-build 
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_4 1> results/2021_batch_1.csv 2> logs/2021_batch_1.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_4 1> results/2021_batch_2.csv 2> logs/2021_batch_2.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_4 1> results/2021_batch_3.csv 2> logs/2021_batch_3.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_4 1> results/2021_batch_4.csv 2> logs/2021_batch_4.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_5 1> results/2021_batch_5.csv 2> logs/2021_batch_5.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_6 1> results/2021_batch_6.csv 2> logs/2021_batch_6.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_7 1> results/2021_batch_7.csv 2> logs/2021_batch_7.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_8 1> results/2021_batch_8.csv 2> logs/2021_batch_8.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_9 1> results/2021_batch_9.csv 2> logs/2021_batch_9.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_9 1> results/2021_batch_A.csv 2> logs/2021_batch_A.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_9 1> results/2021_batch_B.csv 2> logs/2021_batch_B.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_9 1> results/2021_batch_C.csv 2> logs/2021_batch_C.log
	-./Benchmark_Build/JT-2-DT --benchmark 2021_batch_9 1> results/2021_batch_D.csv 2> logs/2021_batch_D.log

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
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_A 1> results/2022_batch_A.csv 2> logs/2022_batch_A.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_B 1> results/2022_batch_B.csv 2> logs/2022_batch_B.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_C 1> results/2022_batch_C.csv 2> logs/2022_batch_C.log
	-./Benchmark_Build/JT-2-DT --benchmark 2022_batch_D 1> results/2022_batch_D.csv 2> logs/2022_batch_D.log