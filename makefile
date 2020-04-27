imdb.jar: cell/main.cell cell/imdb.cell cell/csv.cell
	@rm -rf imdb.jar tmp/gen/ tmp/net/
	@mkdir -p tmp/gen/
	java -jar bin/cellc-java.jar projects/standalone.txt tmp/gen/
	javac -g -d tmp/ tmp/gen/*.java
	jar cfe imdb.jar net.cell_lang.Generated -C tmp net/

imdb.dll: cell/main.cell cell/imdb.cell cell/csv.cell
	@rm -rf dotnet/*.cs dotnet/bin/ dotnet/obj/ tmp/
	@mkdir tmp/
	dotnet bin/cellc-cs.dll projects/standalone.txt tmp/
	mv tmp/generated.cs tmp/runtime.cs dotnet/
	dotnet build -c Release dotnet/

imdb-java.jar: java/imdb.java
	@rm -rf imdb-java.jar tmp/java
	@mkdir -p tmp/java
	javac -d tmp/java/ java/imdb.java
	jar cfe imdb-java.jar IMDB -C tmp/java /

imdb-cs.dll: csharp/imdb.cs csharp/imdb.csproj
	@rm -rf imdb-cs.dll csharp/bin csharp/obj
	cd csharp ; dotnet build -c Release
	ln -s csharp/bin/Release/netcoreapp3.1/imdb-cs.dll .

imdb-embedded.jar: cell/main.java cell/imdb.cell cell/csv.cell cell/embedded.cell
	@rm -rf imdb-embedded.jar tmp/gen tmp/net/
	@mkdir -p tmp/gen/
	java -jar bin/cellc-java.jar projects/embedded.txt tmp/gen/
	javac -g -d tmp/ cell/main.java tmp/gen/*.java
	jar cfe imdb-embedded.jar IMDB -C tmp net/ `cd tmp/ ; ls *.class | sed 's/^/ -C tmp /'`

imdb-embedded.dll: cell/main.cs cell/imdb.cell cell/csv.cell cell/embedded.cell
	@rm -rf dotnet/*.cs dotnet/bin/ dotnet/obj/ tmp/
	@mkdir -p tmp/
	dotnet bin/cellc-cs.dll projects/embedded.txt tmp/
	cp cell/main.cs dotnet/
	mv tmp/generated.cs tmp/runtime.cs tmp/automata.cs tmp/typedefs.cs dotnet/
	dotnet build -c Release dotnet/

clean:
	@rm -rf tmp/* imdb.jar imdb-embedded.jar imdb-java.jar imdb-cs.dll csharp/bin csharp/obj
	@rm -rf dotnet/*.cs dotnet/bin/ dotnet/obj/
	@rm -rf debug/*
