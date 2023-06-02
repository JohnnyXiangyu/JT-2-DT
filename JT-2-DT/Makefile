benchmark-build: JT-2-DT.csproj
	dotnet build JT-2-DT.csproj -c Release
	cp bin/Release/net6.0/JT* Benchmark_Build/
	cp -r Examples Benchmark_Build/
	cp -r external_executables Benchmark_Build/

benchmark: benchmark-build 
	./Benchmark_Build/JT-2-DT --benchmark > Benchmark_Build/bench.mark
	