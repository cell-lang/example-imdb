using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


class IMDB {
  static void Main(string[] args) {
    try {
      if (args.Length == 3) {
        String option = args[0];
        int repetitions = Int32.Parse(args[1]);
        String path = args[2];

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
        else {
          PrintUsage();
        }
      }
      else {
        PrintUsage();
      }
    }
    catch (Exception e) {
      Console.WriteLine(e.ToString());
      Console.WriteLine();
      PrintUsage();
    }
  }

  static void PrintUsage() {
    Console.WriteLine("Usage: dotnet imdb.dll [-l|-u|-q|-uq] <repetitions> <input directory>");
    Console.WriteLine("  -l   load dataset only");
    Console.WriteLine("  -u   run updates");
    Console.WriteLine("  -q   run queries");
    Console.WriteLine("  -uq  run queries on updated dataset\n");
  }

  static void RunTests(String path, int numOfQueryRuns, bool runUpdates) {
    MoviesDB moviesDB = new MoviesDB();

    bool noQueries = numOfQueryRuns == 0;

    ReadMovies(moviesDB, path, false, noQueries ? 5 : 0);
    ReadActors(moviesDB, path, true, noQueries ? 5 : 0);
    ReadDirectors(moviesDB, path, true, noQueries ? 5 : 0);
    ReadMoviesDirectors(moviesDB, path, true, noQueries ? 5 : 0);
    ReadMoviesGenres(moviesDB, path, true, noQueries ? 5 : 0);
    ReadRoles(moviesDB, path, true, noQueries ? 6 : 0);

    if (!runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          Console.Write("\n");
        RunQueries(moviesDB);
      }

    if (runUpdates) {
      BumpUpRankOfMoviesMadeInOrBefore(moviesDB, new int[] {1970, 1989, 2000},
                                        new double[] {0.2, 0.05, 0.05}, true, noQueries ? 6 : 0);

      CalcActorsAvgsMoviesRanks(moviesDB, true, noQueries ? 4 : 0);
      CalcDirectorsAvgsMoviesRanks(moviesDB, true, noQueries ? 4 : 0);

      BumpUpRankOfMovieAndAllItsActorsAndDirectors(moviesDB, 0.1, true, noQueries ? 5 : 0);

      DeleteMoviesWithRankBelow(moviesDB, 4.0, true, noQueries ? 5 : 0);
      DeleteActorsWithNoRoles(moviesDB, true, noQueries ? 4 : 0);
      DeleteDirectorsWithNoMovies(moviesDB, true, noQueries ? 4 : 0);
    }

    if (runUpdates)
      for (int i=0 ; i < numOfQueryRuns ; i++) {
        if (i > 0)
          Console.Write("\n");
        RunQueries(moviesDB);
      }

    Console.WriteLine();
  }

  static void RunQueries(MoviesDB moviesDB) {
    NumOfMoviesWithRankAbove(moviesDB, false, 4);
    NumOfActorsWhoPlayedInAMovieWithRankAbove(moviesDB, true, 5);
    CoActorsInMoviesWithRankAbove(moviesDB, true, 5);
    MoviesAgeHistogram(moviesDB, true, 4);
    AvgAgeOfMoviesWithRankAbove(moviesDB, true, 4);
    SumOfAllMoviesAges(moviesDB, true, 4);

    MoviesWithActorsInCommon(moviesDB, true, 5);
    UniqueLastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 5);

    CoActorsWithCountInMoviesWithRankAbove(moviesDB, true, 5);
    LastNamesOfActorsWithSameFirstNameAs(moviesDB, true, 5);
    IsAlsoActor(moviesDB, true, 4);
    FullName(moviesDB, true, 4);
  }

  //////////////////////////////////////////////////////////////////////////////

  static void ReadMovies(MoviesDB moviesDB, String path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/movies.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int id = (int) reader.ReadLong();
      reader.Skip(';');
      String name = reader.ReadString();
      reader.Skip(';');
      int year = (int) reader.ReadLong();
      reader.Skip(';');
      double rank = reader.ReadDouble();
      reader.SkipLine();

      moviesDB.AddMovie(id, name, year, rank);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadActors(MoviesDB moviesDB, String path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/actors.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int id = (int) reader.ReadLong();
      reader.Skip(';');
      String firstName = reader.ReadString();
      reader.Skip(';');
      String lastName = reader.ReadString();
      reader.Skip(';');
      String genderStr = reader.ReadString();
      reader.SkipLine();

      Actor.Gender gender;
      if (genderStr.Equals("M"))
        gender = Actor.Gender.male;
      else if (genderStr.Equals("F"))
        gender = Actor.Gender.female;
      else
        throw new Exception();

      moviesDB.AddActor(id, firstName, lastName, gender);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadDirectors(MoviesDB moviesDB, String path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/directors.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int id = (int) reader.ReadLong();
      reader.Skip(';');
      String firstName = reader.ReadString();
      reader.Skip(';');
      String lastName = reader.ReadString();
      reader.SkipLine();

      moviesDB.AddDirector(id, firstName, lastName);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadMoviesDirectors(MoviesDB moviesDB, String path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/movies_directors.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int directorId = (int) reader.ReadLong();
      reader.Skip(';');
      int movieId = (int) reader.ReadLong();
      reader.SkipLine();

      moviesDB.AddMovieDirector(directorId, movieId);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadMoviesGenres(MoviesDB moviesDB, String path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/movies_genres.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int movieId = (int) reader.ReadLong();
      reader.Skip(';');
      String genre = reader.ReadString();
      reader.SkipLine();

      moviesDB.AddMovieGenre(movieId, genresMap[genre]);
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void ReadRoles(MoviesDB moviesDB, String path, bool printSep, int width) {
    byte[] content = File.ReadAllBytes(path + "/roles.csv");

    long msecs1 = Environment.TickCount;

    CsvReader reader = new CsvReader(content);
    reader.SkipLine();
    while (!reader.Eof()) {
      int actorId = (int) reader.ReadLong();
      reader.Skip(';');
      int movieId = (int) reader.ReadLong();
      reader.Skip(';');
      String role = reader.ReadString();
      reader.SkipLine();

      moviesDB.AddRole(actorId, movieId, role);
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
    moviesDB.CalcActorsAvgsMoviesRanks();
    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void CalcDirectorsAvgsMoviesRanks(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;
    moviesDB.CalcDirectorsAvgsMoviesRanks();
    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void BumpUpRankOfMovieAndAllItsActorsAndDirectors(MoviesDB moviesDB, double factor, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxId = 0;
    foreach (Movie m in moviesDB.movies.Values)
      if (m.id > maxId)
        maxId = m.id;
    int numOfIds = moviesDB.movies.Count / 4;
    int[] randomIds = RandomInts(maxId, numOfIds, 735025);

    foreach (int id in randomIds) {
      Movie movie;
      if (moviesDB.movies.TryGetValue(id, out movie))
        moviesDB.BumpUpRankOfMovieAndAllItsActorsAndDirectors(movie, factor);
    }

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

    int maxId = 0;
    foreach (Movie m in moviesDB.movies.Values)
      if (m.id > maxId)
        maxId = m.id;
    int numOfIds = moviesDB.movies.Count / 6;
    int[] randomIds = RandomInts(maxId, numOfIds, 64798);

    long count = 0;
    int misses = 0;

    foreach (int id in randomIds) {
      Movie movie;
      if (moviesDB.movies.TryGetValue(id, out movie)) {
        HashSet<Movie> movies = movie.MoviesWithActorsInCommon();
        count += movies.Count;
      }
      else
        misses++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void MoviesAgeHistogram(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int[] histogram;
    for (int i=0 ; i < 50 ; i++)
      histogram = moviesDB.MoviesAgeHistogram(1900, 5.0 + i * 0.1);

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void IsAlsoActor(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long count = 0;
    long otherCount = 0;
    foreach (Director d in moviesDB.directors.Values) {
      if (moviesDB.IsAlsoActor(d))
        count++;
      else
        otherCount++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void FullName(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long len = 0;
    foreach (Actor a in moviesDB.actors.Values) {
      String fullName = a.FullName();
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
      counts[i] = moviesDB.NumOfMoviesWithRankAbove((i+1) * 0.1);
    ;
    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void NumOfActorsWhoPlayedInAMovieWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;
    int[] counts = new int[50];
    for (int i=0 ; i < 50 ; i++)
      counts[i] = moviesDB.NumOfActorsWhoPlayedInAMovieWithRankAbove((i+1) * 0.2);
    ;
    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void CoActorsInMoviesWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    long maxCoActors = 0;

    foreach (Actor a in moviesDB.actors.Values) {
      // Actor[] coActors = moviesDB.CoActorsInMoviesWithRankAbove(a, 6.0);
      // if (coActors.Length > maxCoActors)
      //   maxCoActors = coActors.Length;
      HashSet<Actor> coActors = moviesDB.CoActorsInMoviesWithRankAbove(a, 6.0);
      if (coActors.Count > maxCoActors)
        maxCoActors = coActors.Count;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void CoActorsWithCountInMoviesWithRankAbove(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxActorId = 0;
    foreach (Actor a in moviesDB.actors.Values)
      if (a.id > maxActorId)
        maxActorId = a.id;
    int numOfIds = moviesDB.actors.Count / 4;
    int[] randomIds = RandomInts(maxActorId, numOfIds, 72594);

    long maxCoActors = 0;
    int misses = 0;
    foreach (int id in randomIds) {
      Actor actor;
      if (moviesDB.actors.TryGetValue(id, out actor)) {
        Dictionary<Actor, int> coActors = moviesDB.CoActorsWithCountInMoviesWithRankAbove(actor, 6.0);
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

    int maxActorId = 0;
    foreach (Actor a in moviesDB.actors.Values)
      if (a.id > maxActorId)
        maxActorId = a.id;
    int numOfIds = moviesDB.actors.Count / 10;
    int[] randomIds = RandomInts(maxActorId, numOfIds, 35102);

    int maxNo = 0;
    int misses = 0;
    foreach (int id in randomIds) {
      Actor actor;
      if (moviesDB.actors.TryGetValue(id, out actor)) {
        List<String> actors = moviesDB.LastNamesOfActorsWithSameFirstNameAs(actor);
        if (actors.Count > maxNo)
          maxNo = actors.Count;
      }
      else
        misses++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  static void UniqueLastNamesOfActorsWithSameFirstNameAs(MoviesDB moviesDB, bool printSep, int width) {
    long msecs1 = Environment.TickCount;

    int maxActorId = 0;
    foreach (Actor a in moviesDB.actors.Values)
      if (a.id > maxActorId)
        maxActorId = a.id;
    int numOfIds = moviesDB.actors.Count / 20;
    int[] randomIds = RandomInts(maxActorId, numOfIds, 35102);

    int maxNo = 0;
    int misses = 0;
    foreach (int id in randomIds) {
      Actor actor;
      if (moviesDB.actors.TryGetValue(id, out actor)) {
        HashSet<String> actors = moviesDB.UniqueLastNamesOfActorsWithSameFirstNameAs(actor);
        if (actors.Count > maxNo)
          maxNo = actors.Count;
      }
      else
        misses++;
    }

    long msecs2 = Environment.TickCount;
    PrintTime(msecs2 - msecs1, printSep, width);
  }

  // static void UniqueLastNamesOfActorsWithSameFirstNameAs(MoviesDB moviesDB, bool printSep, int width) {
  //   long msecs1 = Environment.TickCount;
  //   int maxNo = 0;
  //   foreach (Actor a in moviesDB.actors.Values) {
  //     HashSet<String> actors = moviesDB.UniqueLastNamesOfActorsWithSameFirstNameAs(a);
  //     if (actors.Count > maxNo)
  //       maxNo = actors.Count;
  //   }
  //   long msecs2 = Environment.TickCount;
  //   PrintTime(msecs2 - msecs1, printSep, width);
  // }

  //////////////////////////////////////////////////////////////////////////////

  static Dictionary<String, Movie.Genre> genresMap;

  static IMDB() {
    genresMap = new Dictionary<String, Movie.Genre>();
    genresMap["Action"]      = Movie.Genre.action;
    genresMap["Adult"]       = Movie.Genre.adult;
    genresMap["Adventure"]   = Movie.Genre.adventure;
    genresMap["Animation"]   = Movie.Genre.animation;
    genresMap["Comedy"]      = Movie.Genre.comedy;
    genresMap["Crime"]       = Movie.Genre.crime;
    genresMap["Documentary"] = Movie.Genre.documentary;
    genresMap["Drama"]       = Movie.Genre.drama;
    genresMap["Family"]      = Movie.Genre.family;
    genresMap["Fantasy"]     = Movie.Genre.fantasy;
    genresMap["Film-Noir"]   = Movie.Genre.filmNoir;
    genresMap["Horror"]      = Movie.Genre.horror;
    genresMap["Music"]       = Movie.Genre.music;
    genresMap["Musical"]     = Movie.Genre.musical;
    genresMap["Mystery"]     = Movie.Genre.mystery;
    genresMap["Romance"]     = Movie.Genre.romance;
    genresMap["Sci-Fi"]      = Movie.Genre.sciFi;
    genresMap["Short"]       = Movie.Genre._short;
    genresMap["Thriller"]    = Movie.Genre.thriller;
    genresMap["War"]         = Movie.Genre.war;
    genresMap["Western"]     = Movie.Genre.western;
  }

  //////////////////////////////////////////////////////////////////////////////

  static void PrintTime(long time, bool printSep, int width) {
    if (width > 0) {
      if (printSep)
        Console.Write(",");
      String str = time.ToString();
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
      Check(NextIs(';'));
      Read();
    }
  }

  Object ReadField() {
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

  public double ReadDouble() {
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

  Object ReadNumber() {
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

  public String ReadString() {
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

  char Read() {
    return (char) content[index++];
  }

  char Peek() {
    return (char) content[index];
  }

  bool NextIs(char ch) {
    return index < content.Length && content[index] == ch;
  }

  public bool Eof() {
    return index >= content.Length;
  }

  void Check(bool cond) {
    if (!cond)
      throw new Exception();
  }
}

////////////////////////////////////////////////////////////////////////////////

class Movie {
  public enum Genre {
    action,
    adult,
    adventure,
    animation,
    comedy,
    crime,
    documentary,
    drama,
    family,
    fantasy,
    filmNoir,
    horror,
    music,
    musical,
    mystery,
    romance,
    sciFi,
    _short,
    thriller,
    war,
    western
  };

  public int    id;
  public String name;
  public int    year;
  public double rank;

  public List<Genre> genres = new List<Genre>();
  public List<Role>  actors = new List<Role>();
  public List<Director> directors = new List<Director>();

  public Movie(int id, String name, int year, double rank) {
    this.id = id;
    this.name = name;
    this.year = year;
    this.rank = rank;
  }

  public void Add(Genre genre) {
    genres.Add(genre);
  }

  public void Add(Role role) {
    actors.Add(role);
  }

  public void Add(Director director) {
    directors.Add(director);
  }

  public int Age(int currYear) {
    return currYear - year;
  }

  public HashSet<Movie> MoviesWithActorsInCommon() {
    HashSet<Movie> movies = new HashSet<Movie>();
    foreach (Role r1 in actors)
      foreach (Role r2 in r1.actor.roles)
        if (r2.movie != this)
          movies.Add(r2.movie);
    return movies;
  }
}


class Actor {
  public enum Gender {male, female};

  public int    id;
  public String firstName;
  public String lastName;
  public Gender gender;
  public double avgMoviesRank = -1.0;

  public List<Role> roles = new List<Role>();

  public Actor(int id, String firstName, String lastName, Gender gender) {
    this.id = id;
    this.firstName = firstName;
    this.lastName = lastName;
    this.gender = gender;
  }

  public void Add(Role role) {
    roles.Add(role);
  }

  public void Remove(Role role) {
    roles.Remove(role);
  }

  public String FullName() {
    return firstName + " " + lastName;
  }

  public void CalcAvgsMoviesRank() {
    int count = 0;
    double sum = 0.0;
    foreach (Role r in roles) {
      double rank = r.movie.rank;
      if (rank > 0) {
        count++;
        sum += rank;
      }
    }
    if (count > 0)
      avgMoviesRank = sum / count;
  }
}


class Role {
  public Movie  movie;
  public Actor  actor;
  public String role;

  public Role(Movie movie, Actor actor, String role) {
    this.movie = movie;
    this.actor = actor;
    this.role = role;
    movie.Add(this);
    actor.Add(this);
  }
}


class Director {
  public int    id;
  public String firstName;
  public String lastName;
  public double avgMoviesRank = -1.0;

  public List<Movie> movies = new List<Movie>();

  public Director(int id, String firstName, String lastName) {
    this.id = id;
    this.firstName = firstName;
    this.lastName = lastName;
  }

  public void Add(Movie movie) {
    movies.Add(movie);
    movie.Add(this);
  }

  public void Remove(Movie movie) {
    movies.Remove(movie);
  }

  public void CalcAvgsMoviesRank() {
    int count = 0;
    double sum = 0.0;
    foreach (Movie m in movies)
      if (m.rank > 0) {
        count++;
        sum += m.rank;
      }
    if (count > 0)
      avgMoviesRank = sum / count;
  }
}

////////////////////////////////////////////////////////////////////////////////

class MoviesDB {
  public Dictionary<int, Movie> movies = new Dictionary<int, Movie>();
  public Dictionary<int, Actor> actors = new Dictionary<int, Actor>();
  public Dictionary<int, Director> directors = new Dictionary<int, Director>();

  public Dictionary<String, HashSet<Actor>> actorsByFirstName = new Dictionary<String, HashSet<Actor>>();
  public Dictionary<String, HashSet<Actor>> actorsByLastName = new Dictionary<String, HashSet<Actor>>();


  public void AddMovie(int id, String name, int year, double rank) {
    movies[id] = new Movie(id, name, year, rank);
  }

  public void AddActor(int id, String firstName, String lastName, Actor.Gender gender) {
    Actor actor = new Actor(id, firstName, lastName, gender);
    actors[id] = actor;

    HashSet<Actor> sameNameActors;
    if (!actorsByFirstName.TryGetValue(firstName, out sameNameActors)) {
      sameNameActors = new HashSet<Actor>();
      actorsByFirstName[firstName] = sameNameActors;
    }
    sameNameActors.Add(actor);

    if (!actorsByLastName.TryGetValue(lastName, out sameNameActors)) {
      sameNameActors = new HashSet<Actor>();
      actorsByLastName[lastName] = sameNameActors;
    }
    sameNameActors.Add(actor);
  }

  public void AddDirector(int id, String firstName, String lastName) {
    directors[id] = new Director(id, firstName, lastName);
  }

  public void AddMovieGenre(int movieId, Movie.Genre genre) {
    movies[movieId].Add(genre);
  }

  public void AddMovieDirector(int directorId, int movieId) {
    directors[directorId].Add(movies[movieId]);
  }

  public void AddRole(int actorId, int movieId, String roleDescr) {
    new Role(movies[movieId], actors[actorId], roleDescr);
  }

  //////////////////////////////////////////////////////////////////////////////

  public void BumpUpRankOfMoviesMadeInOrBefore(int year, double factor) {
    foreach (Movie m in movies.Values)
      if (m.year <= year)
        m.rank += factor * (10.0 - m.rank);
  }

  //////////////////////////////////////////////////////////////////////////////

  public void CalcActorsAvgsMoviesRanks() {
    foreach (Actor a in actors.Values)
      a.CalcAvgsMoviesRank();
  }

  public void CalcDirectorsAvgsMoviesRanks() {
    foreach (Director d in directors.Values)
      d.CalcAvgsMoviesRank();
  }

  //////////////////////////////////////////////////////////////////////////////

  public void BumpUpRankOfMovieAndAllItsActorsAndDirectors(Movie movie, double factor) {
    double delta = factor * (10.0 - movie.rank);
    movie.rank += delta;

    foreach (Role r in movie.actors) {
      Actor actor = r.actor;
      if (actor.avgMoviesRank > 0.0) {
        int count = actor.roles.Count;
        actor.avgMoviesRank += delta / count;
      }
    }

    foreach (Director d in movie.directors)
      if (d.avgMoviesRank > 0.0) {
        int count = d.movies.Count;
        d.avgMoviesRank += delta / count;
      }
  }

  public void DeleteMoviesWithRankBelow(double rank) {
    List<Movie> moviesToRemove = new List<Movie>();
    foreach (Movie m in movies.Values)
      if (m.rank < rank)
        moviesToRemove.Add(m);
    foreach (Movie m in moviesToRemove) {
      if (!movies.Remove(m.id))
        throw new Exception();
      foreach (Role r in m.actors)
        r.actor.Remove(r);
      foreach (Director d in m.directors)
        d.Remove(m);
    }
  }

  public void DeleteActorsWithNoRoles() {
    List<Actor> actorsToRemove = new List<Actor>();
    foreach (Actor a in actors.Values)
      if (a.roles.Count == 0)
        actorsToRemove.Add(a);
    foreach (Actor a in actorsToRemove) {
      if (!actors.Remove(a.id))
        throw new Exception();
    }
  }

  public void DeleteDirectorsWithNoMovies() {
    List<Director> directorsToRemove = new List<Director>();
    foreach (Director d in directors.Values)
      if (d.movies.Count == 0)
        directorsToRemove.Add(d);
    foreach (Director d in directorsToRemove) {
      if (!directors.Remove(d.id))
        throw new Exception();
    }
  }

  //////////////////////////////////////////////////////////////////////////////

  public int NumOfMoviesWithRankAbove(double minRank) {
    int count = 0;
    foreach (Movie movie in movies.Values)
      if (movie.rank >= minRank)
        count++;
    return count;
  }

  public int NumOfActorsWhoPlayedInAMovieWithRankAbove(double minRank) {
    int count = 0;
    foreach (Actor actor in actors.Values)
      foreach (Role role in actor.roles)
        if (role.movie.rank >= minRank) {
          count++;
          break;
        }
    return count;
  }

  // public Actor[] CoActorsInMoviesWithRankAbove(Actor actor, double minRank) {
  public HashSet<Actor> CoActorsInMoviesWithRankAbove(Actor actor, double minRank) {
    HashSet<Actor> coActors = new HashSet<Actor>();
    foreach (Role r1 in actor.roles)
      if (r1.movie.rank >= minRank)
        foreach (Role r2 in r1.movie.actors)
          if (r2.actor != actor)
            coActors.Add(r2.actor);
    // return coActors.ToArray(new Actor[0]);
    return coActors;
  }

  public Dictionary<Actor, int> CoActorsWithCountInMoviesWithRankAbove(Actor actor, double minRank) {
    Dictionary<Actor, int> coActors = new Dictionary<Actor, int>();
    foreach (Role r1 in actor.roles)
      if (r1.movie.rank >= minRank)
        foreach (Role r2 in r1.movie.actors)
          if (r2.actor != actor) {
            int count;
            coActors.TryGetValue(r2.actor, out count);
            coActors[r2.actor] = count + 1;
          }
    return coActors;
  }

  public List<String> LastNamesOfActorsWithSameFirstNameAs(Actor actor) {
    HashSet<Actor> actors = actorsByFirstName[actor.firstName];
    List<String> lastNames = new List<String>();
    if (actors != null)
      foreach (Actor a in actors)
        if (a != actor)
          lastNames.Add(a.lastName);
    return lastNames;
  }

  public HashSet<String> UniqueLastNamesOfActorsWithSameFirstNameAs(Actor actor) {
    HashSet<Actor> actors = actorsByFirstName[actor.firstName];
    HashSet<String> lastNames = new HashSet<String>();
    if (actors != null)
      foreach (Actor a in actors)
        if (a != actor)
          lastNames.Add(a.lastName);
    return lastNames;
  }

  public bool IsAlsoActor(Director director) {
    HashSet<Actor> actors;
    if (actorsByLastName.TryGetValue(director.lastName, out actors))
      foreach (Actor a in actors)
        if (a.firstName == director.firstName)
          return true;
    return false;
  }

  public int[] MoviesAgeHistogram(int startYear, double minRank) {
    int[] histogram = new int[0];
    foreach (Movie m in movies.Values)
      if (m.rank >= minRank && m.year >= startYear) {
        int idx = m.year - startYear;
        if (idx >= histogram.Length) {
          int[] newHistogram = new int[idx+1];
          Array.Copy(histogram, newHistogram, histogram.Length);
          histogram = newHistogram;
        }
        histogram[idx]++;
      }
    return histogram;
  }

  public double AvgAgeOfMoviesWithRankAbove(int currYear, double minRank) {
    long totalAge = 0;
    int count = 0;
    foreach (Movie m in movies.Values) {
      if (m.rank >= minRank) {
        int age = m.Age(currYear);
        totalAge += age;
        count++;
      }
    }
    return totalAge / (double) count;
  }

  public long SumOfAllMoviesAges(int currYear) {
    long totalAge = 0;
    foreach (Movie m in movies.Values) {
      int age = m.Age(currYear);
      totalAge += age;
    }
    return totalAge;
  }
}

