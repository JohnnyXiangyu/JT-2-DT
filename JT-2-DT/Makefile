benchmark-build: JT-2-DT.csproj
	dotnet build JT-2-DT.csproj -c Release
	rm -rf Benchmark_Build/
	mkdir Benchmark_Build/
	cp bin/Release/net6.0/JT* Benchmark_Build/
	cp -r Examples Benchmark_Build/
	cp -r external_executables Benchmark_Build/

benchmark: benchmark-build 
	./Benchmark_Build/JT-2-DT --benchmark c2d 1> Benchmark_Build/bench_results.csv 2> Benchmark_Build/bench_log.log 
	