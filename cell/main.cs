using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Cell.Facades;


class IMDB {
  // static int counter;

  public static void Main(string[] args) {
    int repetitions;

    if (args.Length == 3 | args.Length == 4) {
      try {
        repetitions = Int32.Parse(args[1]);
      }
      catch (Exception e) {
        PrintUsage();
        Console.WriteLine();
        throw e;
      }

      string option = args[0];
      string path = args[2];

      if (args.Length == 4) {
        string outFile = args[3];

        if (option.Equals("-w")) {
          RunStoringTest(path, outFile, false, repetitions);
        }
        else if (option.Equals("-uw")) {
          RunStoringTest(path, outFile, true, repetitions);
        }
        else {
          PrintUsage();
        }
        return;
      }

      if (option.Equals("-l")) {
        for (int i=0 ; i < repetitions ; i++)
          RunTests(path, 0, false);
      }
      else if (option.Equals("-u")) {
        for (int i=0 ; i < repetitions ; i++)
          RunTests(path, 0, true);
      }
      else if (option.Equals("-q")) {
        RunTests(path, repetitions, false);
      }
      else if (option.Equals("-uq")) {
        RunTests(path, repetitions, true);
      }
      else if (option.Equals("-r")) {
        for (int i=0 ; i < repetitions ; i++)
          RunLoadingTests(path);
      }
      else {
        PrintUsage();
      }
    }
    else {
      PrintUsage();
    }
  }

  static void PrintUsage() {
    Console.WriteLine("Usage: java -jar imdb-embedded.jar [-l|-u|-q|-uq] <repetitions> <input directory>");
    Console.WriteLine("  -l   load dataset from csv files");
    Console.WriteLine("  -u   run updates");
    Console.WriteLine("  -q   run queries");
    Console.WriteLine("  -uq  run queries on updated dataset");
    Console.WriteLine();
    Console.WriteLine("or: java -jar imdb-embedded.jar [-w|-uw] <repetitions> <input directory> <output file>");
    Console.WriteLine("  -w   load dataset and write state to specified output file");
    Console.WriteLine("  -uw  load dataset and write updated state to specified output file");
    Console.WriteLine();
    Console.WriteLine("or: java -jar imdb-embedded.jar [-r] <repetitions> <input file>");
    Console.WriteLine("  -r   read a previously saved (with the -w or -uw options) state");
  }

  static void RunLoadingTests(string inputFile) {
    MoviesDB moviesDB = new MoviesDB();

    long msecs1 = Environment.TickCount;

    using (Stream stream = new FileStream(inputFile, FileMode.Open)) {
      moviesDB.Load(stream);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, false, 6);
    Console.WriteLine();
  }

  static void RunStoringTest(string inputPath, string outputFile, bool runUpdates, int repetitions) {
    MoviesDB moviesDB = new MoviesDB();

    ReadCsvFiles(moviesDB, inputPath, false);

    if (runUpdates)
      RunUpdates(moviesDB, false);

    for (int i=0 ; i < repetitions ; i++) {
      long msecs1 = Environment.TickCount;

      string file = outputFile;
      if (repetitions > 1 && file.EndsWith(".txt")) {
        file = file.Substring(0, file.Length - 4) + string.Format("-{0:D2}", i) + ".txt";
      }

      using (Stream stream = new FileStream(file, FileMode.Create)) {
        moviesDB.Save(stream);
      }

      long msecs2 = Environment.TickCount;
      PrintTime(msecs2 - msecs1, false, 6);
      Console.WriteLine();
    }
  }

  static void RunTests(string path, int numOfQueryRuns, bool runUpdates) {
    MoviesDB moviesDB = new MoviesDB();

    bool noQueries = numOfQueryRuns == 0;

    ReadCsvFiles(moviesDB, path, noQueries);

    if (!runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          Console.Write("\n");
        RunQueries(moviesDB);
      }

    if (runUpdates)
      RunUpdates(moviesDB, noQueries);

    if (runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          Console.Write("\n");
        RunQueries(moviesDB);
      }

    Console.WriteLine();
  }

  static void ReadCsvFiles(MoviesDB moviesDB, string path, bool printTimes) {
    ReadMovies(moviesDB, path, false, printTimes ? 5 : 0);
    ReadActors(moviesDB, path, true, printTimes ? 5 : 0);
    ReadDirectors(moviesDB, path, true, printTimes ? 5 : 0);
    ReadMoviesDirectors(moviesDB, path, true, printTimes ? 5 : 0);
    ReadMoviesGenres(moviesDB, path, true, printTimes ? 5 : 0);
    ReadRoles(moviesDB, path, true, printTimes ? 6 : 0);
  }

  static void RunUpdates(MoviesDB moviesDB, bool printTimes) {
    BumpUpRankOfMoviesMadeInOrBefore(moviesDB, new int[] {1970, 1989, 2000},
                                      new double[] {0.2, 0.05, 0.05}, true, printTimes ? 6 : 0);

    CalcActorsAvgsMoviesRanks(moviesDB, true, printTimes ? 4 : 0);
    CalcDirectorsAvgsMoviesRanks(moviesDB, true, printTimes ? 4 : 0);

    BumpUpRankOfMovieAndAllItsActorsAndDirectors(moviesDB, 0.1, true, printTimes ? 5 : 0);

    DeleteMoviesWithRankBelow(moviesDB, 4.0, true, printTimes ? 5 : 0);
    DeleteActorsWithNoRoles(moviesDB, true, printTimes ? 4 : 0);
    DeleteDirectorsWithNoMovies(moviesDB, true, printTimes ? 4 : 0);
  }

  static void RunQueries(MoviesDB moviesDB) {
    NumOfMoviesWithRankAbove(moviesDB, false, 4);
    NumOfActorsWhoPlayedInAMovieWithRankAbove(moviesDB, true, 6);
    CoActorsInMoviesWithRankAbove(moviesDB, true, 5);
    MoviesAgeHistogram(moviesDB, true, 3);
    AvgAgeOfMoviesWithRankAbove(moviesDB, true, 3);
    SumOfAllMoviesAges(moviesDB, true, 3);

    MoviesWithActorsInCommon(moviesDB, true, 6);
    UniqueLastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 6);

    CoActorsWithCountInMoviesWithRankAbove(moviesDB, true, 6);
    LastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 7);
    IsAlsoActor(moviesDB, true, 4);
    DirectorsWhoAreAlsoActors(moviesDB, false, 5);
    FullName(moviesDB, true, 5);
  }

  //////////////////////////////////////////////////////////////////////////////

  static void ReadMovies(MoviesDB moviesDB, string path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/movies.csv");

    long msecs1 = Environment.TickCount;

    Genre[] empty = new Genre[0];

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int id = (int) reader.ReadLong();
      reader.Skip(';');
      string name = reader.ReadString();
      reader.Skip(';');
      int year = (int) reader.ReadLong();
      reader.Skip(';');
      double rank = reader.ReadDouble();
      reader.SkipLine();

      moviesDB.AddMovie(id, name, year, rank, empty);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadActors(MoviesDB moviesDB, string path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/actors.csv");

    long msecs1 = Environment.TickCount;

    bool indexCreationTriggered = false;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int id = (int) reader.ReadLong();
      reader.Skip(';');
      string firstName = reader.ReadString();
      reader.Skip(';');
      string lastName = reader.ReadString();
      reader.Skip(';');
      string genderStr = reader.ReadString();
      reader.SkipLine();

      Gender gender;
      if (genderStr.Equals("M"))
        gender = Male.singleton;
      else if (genderStr.Equals("F"))
        gender = Female.singleton;
      else
        throw new Exception();

      moviesDB.AddActor(id, firstName, lastName, gender);

      if (!indexCreationTriggered) {
        moviesDB.ActorsByFirstName("...");
        moviesDB.ActorsByLastName("...");
        indexCreationTriggered = true;
      }
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadDirectors(MoviesDB moviesDB, string path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/directors.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int id = (int) reader.ReadLong();
      reader.Skip(';');
      string firstName = reader.ReadString();
      reader.Skip(';');
      string lastName = reader.ReadString();
      reader.SkipLine();

      moviesDB.AddDirector(id, firstName, lastName);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadMoviesDirectors(MoviesDB moviesDB, string path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/movies_directors.csv");

    long msecs1 = Environment.TickCount;

    bool indexCreationTriggered = false;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int directorId = (int) reader.ReadLong();
      reader.Skip(';');
      int movieId = (int) reader.ReadLong();
      reader.SkipLine();

      moviesDB.AddMovieDirector(movieId, directorId);

      if (!indexCreationTriggered) {
        moviesDB.DirectorsOf(0);
        indexCreationTriggered = true;
      }
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadMoviesGenres(MoviesDB moviesDB, string path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/movies_genres.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int movieId = (int) reader.ReadLong();
      reader.Skip(';');
      string genre = reader.ReadString();
      reader.SkipLine();

      moviesDB.AddMovieGenre(movieId, genresMap[genre]);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadRoles(MoviesDB moviesDB, string path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/roles.csv");

    long msecs1 = Environment.TickCount;

    bool indexCreationTriggered = false;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int actorId = (int) reader.ReadLong();
      reader.Skip(';');
      int movieId = (int) reader.ReadLong();
      reader.Skip(';');
      string role = reader.ReadString();
      reader.SkipLine();

      if (role.Length != 0)
        moviesDB.AddMovieActor(movieId, actorId, role);
      else
        moviesDB.AddMovieActor(movieId, actorId);

      if (!indexCreationTriggered) {
        moviesDB.Cast(0);
        indexCreationTriggered = true;
      }
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void BumpUpRankOfMoviesMadeInOrBefore(MoviesDB moviesDB, int[] years, double[] factors, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    for (int i=0 ; i < years.Length ; i++)
      moviesDB.BumpUpRankOfMoviesMadeInOrBefore(years[i], factors[i]);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void CalcActorsAvgsMoviesRanks(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    moviesDB.CalcActorAvgMoviesRank();

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void CalcDirectorsAvgsMoviesRanks(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    moviesDB.CalcDirectorAvgMoviesRank();

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void BumpUpRankOfMovieAndAllItsActorsAndDirectors(MoviesDB moviesDB, double factor, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxId = (int) moviesDB.MaxMovieId();
    int numOfIds = (int) moviesDB.NumOfMovies() / 4;
    int[] randomIds = RandomInts(maxId, numOfIds, 735025);

    foreach (int id in randomIds)
      if (moviesDB.MovieExists(id))
        moviesDB.BumpUpRankOfMovieAndItsActorsAndDirectors(id, factor);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void DeleteMoviesWithRankBelow(MoviesDB moviesDB, double minRank, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    moviesDB.DeleteMoviesWithRankBelow(minRank);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void DeleteActorsWithNoRoles(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    moviesDB.DeleteActorsWithNoRoles();

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void DeleteDirectorsWithNoMovies(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    moviesDB.DeleteDirectorsWithNoMovies();

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////
  //////////////////////////////////////////////////////////////////////////////

  static void MoviesWithActorsInCommon(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxId = (int) moviesDB.MaxMovieId();
    int numOfIds = (int) moviesDB.NumOfMovies() / 6;
    int[] randomIds = RandomInts(maxId, numOfIds, 64798);

    long count = 0;
    int misses = 0;

    foreach (int id in randomIds) {
      if (moviesDB.MovieExists(id)) {
        long[] movies = moviesDB.MoviesWithActorsInCommon(id);
        count += movies.Length;
      }
      else
        misses++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void MoviesAgeHistogram(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long[] histogram;
    for (int i=0 ; i < 50 ; i++)
      histogram = moviesDB.MovieAgeHistogram(1900, 5.0 + i * 0.1);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void IsAlsoActor(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long count = 0;
    long otherCount = 0;
    foreach (long id in moviesDB.Directors()) {
      if (moviesDB.IsAlsoActor(id))
        count++;
      else
        otherCount++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void DirectorsWhoAreAlsoActors(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long[] directorsActors = moviesDB.DirectorsWhoAreAlsoActors();
    if (directorsActors.Length == 0)
      throw new Exception();

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void FullName(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long len = 0;
    foreach (long id in moviesDB.Actors()) {
      string fullName = moviesDB.FullName(id);
      len += fullName.Length;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void AvgAgeOfMoviesWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    double avgAge;
    for (int i=0 ; i < 50 ; i++)
      avgAge = moviesDB.AvgAgeOfMoviesWithRankAbove(2019, 5.0 + i * 0.1);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void SumOfAllMoviesAges(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long totalAge;
    for (int i=0 ; i < 10 ; i++)
      for (int year=2019 ; year < 2040 ; year++)
        totalAge = moviesDB.SumOfAllMoviesAges(year);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void NumOfMoviesWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int[] counts = new int[100];
    for (int i=0 ; i < 100 ; i++)
      counts[i] = (int) moviesDB.NumOfMoviesWithRankAbove((i+1) * 0.1);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void NumOfActorsWhoPlayedInAMovieWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int[] counts = new int[50];
    for (int i=0 ; i < 50 ; i++)
      counts[i] = (int) moviesDB.NumOfActorsWhoPlayedInAMovieWithRankAbove((i+1) * 0.2);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void CoActorsInMoviesWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long maxCoActors = 0;
    foreach (long id in moviesDB.Actors()) {
      long[] coActors = moviesDB.CoActorsInMoviesWithRankAbove(id, 6.0);
      if (coActors.Length > maxCoActors)
        maxCoActors = coActors.Length;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void CoActorsWithCountInMoviesWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxId = (int) moviesDB.MaxActorId();
    int numOfIds = (int) moviesDB.NumOfActors() / 4;
    int[] randomIds = RandomInts(maxId, numOfIds, 72594);

    long maxCoActors = 0;
    int misses = 0;

    foreach (int id in randomIds) {
      if (moviesDB.ActorExists(id)) {
        Dictionary<string, long> coActors = moviesDB.CoActorsWithCountInMoviesWithRankAbove(id, 6.0);
        if (coActors.Count > maxCoActors)
          maxCoActors = coActors.Count;
      }
      else
        misses++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void LastNamesOfActorsWithSameFirstNameAs(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxId = (int) moviesDB.MaxActorId();
    int numOfIds = (int) moviesDB.NumOfActors() / 10;
    int[] randomIds = RandomInts(maxId, numOfIds, 47619);

    int maxNo = 0;
    int misses = 0;

    foreach (int id in randomIds) {
      if (moviesDB.ActorExists(id)) {
        string[] actors = moviesDB.LastNamesOfActorsWithSameFirstNameAs(id);
        if (actors.Length > maxNo)
          maxNo = actors.Length;
      }
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void UniqueLastNamesOfActorsWithSameFirstNameAs(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxActorId = (int) moviesDB.MaxActorId();
    int numOfIds = (int) moviesDB.NumOfActors() / 20;
    int[] randomIds = RandomInts(maxActorId, numOfIds, 35102);

    int maxNo = 0;
    int misses = 0;

    foreach (int id in randomIds) {
      if (moviesDB.ActorExists(id)) {
        string[] actors = moviesDB.UniqueLastNamesOfActorsWithSameFirstNameAs(id);
        if (actors.Length > maxNo)
          maxNo = actors.Length;
      }
      else
        misses++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  //////////////////////////////////////////////////////////////////////////////

  static Dictionary<string, Genre> genresMap;

  static IMDB() {
    genresMap = new Dictionary<string, Genre>();
    genresMap["Action"]       = Cell.Facades.Action.singleton;
    genresMap["Adult"]        = Adult.singleton;
    genresMap["Adventure"]    = Adventure.singleton;
    genresMap["Animation"]    = Animation.singleton;
    genresMap["Comedy"]       = Comedy.singleton;
    genresMap["Crime"]        = Crime.singleton;
    genresMap["Documentary"]  = Documentary.singleton;
    genresMap["Drama"]        = Drama.singleton;
    genresMap["Family"]       = Family.singleton;
    genresMap["Fantasy"]      = Fantasy.singleton;
    genresMap["Film-Noir"]    = FilmNoir.singleton;
    genresMap["Horror"]       = Horror.singleton;
    genresMap["Music"]        = Music.singleton;
    genresMap["Musical"]      = Musical.singleton;
    genresMap["Mystery"]      = Mystery.singleton;
    genresMap["Romance"]      = Romance.singleton;
    genresMap["Sci-Fi"]       = SciFi.singleton;
    genresMap["Short"]        = Short.singleton;
    genresMap["Thriller"]     = Thriller.singleton;
    genresMap["War"]          = War.singleton;
    genresMap["Western"]      = Western.singleton;
  }

  //////////////////////////////////////////////////////////////////////////////

  static string Escape(string str) {
    return str.Replace("\\", "\\\\").Replace("\"", "\\\"");
  }

  static void PrintTime(long time, bool printSep, int width) {
    if (width > 0) {
      if (printSep)
        Console.Write(",");
      string str = time.ToString();
      while (str.Length < width)
        str = " " + str;
      Console.Write(str);
    }
  }

  static int[] RandomInts(int max, int count, int seed) {
    const long m = 2147483648L;
    const long a = 1103515245;
    const long c = 12345;

    long state = seed;
    int[] ints = new int[count];
    for (int i=0 ; i < count ; i++) {
      state = (a * state + c) % m;
      ints[i] = (int) (state % (max + 1));
    }
    return ints;
  }
}

////////////////////////////////////////////////////////////////////////////////

class CsvReader {
  byte[] content;
  int    index;

  public CsvReader(byte[] content) {
    this.content = content;
    index = 0;
  }

  public void Skip(char ch) {
    if (!NextIs(ch))
      throw new Exception();
    Read();
  }

  public void SkipLine() {
    while (!Eof() && Read() != '\n')
      ;
  }

  public List<Object> ReadRow() {
    List<Object> row = new List<Object>();
    for ( ; ; ) {
      row.Add(ReadField());
      if (Eof())
        return row;
      if (NextIs('\n')) {
        Read();
        return row;
      }
      if (!NextIs(';')) {
        Console.WriteLine("peek() = {0}, row = {1}\n", Peek(), row);
      }
      Check(NextIs(';'));
      Read();
    }
  }

  public Object ReadField() {
    if (Char.IsDigit(Peek()))
      return ReadNumber();
    else if (NextIs('"'))
      return ReadString();
    else
      throw new Exception();
  }

  public long ReadLong() {
    bool neg = NextIs('-');
    if (neg)
      Read();
    Check(Char.IsDigit(Peek()));
    long value = Read() - '0';
    while (!Eof() && Char.IsDigit(Peek()))
      value = 10 * value + Read() - '0';
    return neg ? -value : value;
  }

  public Double ReadDouble() {
    bool neg = NextIs('-');
    if (neg)
      Read();
    Check(Char.IsDigit(Peek()));
    double value = Read() - '0';
    while (!Eof() && Char.IsDigit(Peek()))
      value = 10 * value + Read() - '0';
    if (Eof() || !NextIs('.'))
      return neg ? -value : value;
    Read();
    double weigth = 0.1;
    while (!Eof() && Char.IsDigit(Peek())) {
      value += weigth * (Read() - '0');
      weigth = 0.1 * weigth;
    }
    return value;
  }

  public object ReadNumber() {
    bool neg = NextIs('-');
    if (neg)
      Read();
    Check(Char.IsDigit(Peek()));
    long value = Read() - '0';
    while (!Eof() && Char.IsDigit(Peek()))
      value = 10 * value + Read() - '0';
    if (Eof() || !NextIs('.'))
      return neg ? -value : value;
    Read();
    double floatValue = value;
    double digitValue = 0.1;
    while (!Eof() && Char.IsDigit(Peek())) {
      floatValue += digitValue * (Read() - '0');
      digitValue = 0.1 * digitValue;
    }
    return floatValue;
  }

  public string ReadString() {
    StringBuilder sb = new StringBuilder();
    Check(NextIs('"'));
    Read();
    for ( ; ; ) {
      char ch = Read();
      if (ch == '"')
        if (!NextIs('"'))
          return sb.ToString();
        else
          Read();
      sb.Append(ch);
    }
  }

  public char Read() {
    return (char) content[index++];
  }

  public char Peek() {
    return (char) content[index];
  }

  public bool NextIs(char ch) {
    return index < content.Length && content[index] == ch;
  }

  public bool Eof() {
    return index >= content.Length;
  }

  public void Check(bool cond) {
    if (!cond)
      throw new Exception();
  }
}
