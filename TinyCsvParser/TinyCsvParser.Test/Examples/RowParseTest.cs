﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TinyCsvParser.Mapping;
using TinyCsvParser.Model;

namespace TinyCsvParser.Test.Examples
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int size)
        {
            T[] bucket = null;
            var count = 0;

            foreach (var item in source)
            {
                if (bucket == null)
                {
                    bucket = new T[size];
                }

                bucket[count++] = item;

                if (count != size)
                {
                    continue;
                }

                yield return bucket.Select(x => x);

                bucket = null;
                count = 0;
            }

            if (bucket != null && count > 0)
            {
                yield return bucket.Take(count);
            }
        }
    }

    public class RowBasedCsvParser<TEntity>
    {
        private readonly CsvParserOptions options;
        private readonly ICsvMapping<TEntity> mapping;

        public RowBasedCsvParser(CsvParserOptions options, ICsvMapping<TEntity> mapping)
        {
            this.options = options;
            this.mapping = mapping;
        }

        public IEnumerable<CsvMappingResult<TEntity>> Parse(string rowData, int numberOfProperties)
        {
            if (rowData == null)
            {
                throw new ArgumentNullException(nameof(rowData));
            }

            // This could be too huge for In-Memory. Maybe optimize it, so you 
            // are using IEnumerable:
            var tokens = options.Tokenizer.Tokenize(rowData);

            return tokens
                // Now we Batch the Columns into smaller groups, so they resemble a Row:
                .Batch(numberOfProperties)
                .Select((x, i) => new TokenizedRow(i * numberOfProperties, x.ToArray()))
                .Select(x => mapping.Map(x));
        }

        public override string ToString()
        {
            return $"CsvParser (Options = {options})";
        }
    }

    // Test Entities
    public class TestEntityForRow
    {
        public string A { get; set; }

        public string B { get; set; }
    }

    public class TestEntityForRowMapper : CsvMapping<TestEntityForRow>
    {
        public TestEntityForRowMapper()
        {
            MapProperty(0, x => x.A);
            MapProperty(1, x => x.B);
        }
    }

    [TestFixture]
    public class RowParseTest
    {
        [Test]
        public void TestCustomRowParser()
        {
            string csvRowTestData = "1,2,3,4,5,6,7,8";

            var csvParserOptions = new CsvParserOptions(true, ',');
            var csvReaderOptions = new CsvReaderOptions(new[] { Environment.NewLine });
            var csvMapper = new TestEntityForRowMapper();
            var csvParser = new RowBasedCsvParser<TestEntityForRow>(csvParserOptions, csvMapper);

            var res = csvParser
                .Parse(csvRowTestData, 2)
                .Where(x => x.IsValid)
                .ToList();

            Assert.AreEqual(4, res.Count);

            Assert.AreEqual("1", res[0].Result.A);
            Assert.AreEqual("2", res[0].Result.B);
        }
    }
}
