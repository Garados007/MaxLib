using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using MaxLib.Data.Json;
using MaxLib.Data.Json.Binary;

namespace MaxLib.Test.Data.Json.Binary
{
    [TestClass]
    public class EncoderTest
    {
        const string RawJsonText = @"
{
    ""quiz"": {
        ""sport"": {
            ""q1"": {
                ""question"": ""Which one is correct team name in NBA?"",
                ""options"": [
                    ""New York Bulls"",
                    ""Los Angeles Kings"",
                    ""Golden State Warriros"",
                    ""Huston Rocket""
                ],
                ""answer"": ""Huston Rocket""
            }
        },
        ""maths"": {
            ""q1"": {
                ""question"": ""5 + 7 = ?"",
                ""options"": [
                    ""10"",
                    ""11"",
                    ""12"",
                    ""13""
                ],
                ""answer"": ""12""
            },
            ""q2"": {
                ""question"": ""12 - 8 = ?"",
                ""options"": [
                    ""1"",
                    ""2"",
                    ""3"",
                    ""4""
                ],
                ""answer"": ""4""
            }
        }
    }
}";

        const string LargeExample = @"
[
  {
    ""_id"": ""5dbc756fdc50f43f223d4332"",
    ""index"": 0,
    ""guid"": ""3d0f2fd3-c16d-4ecd-80fd-b46e63c1e472"",
    ""isActive"": false,
    ""balance"": ""$3,435.35"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 22,
    ""eyeColor"": ""blue"",
    ""name"": {
      ""first"": ""Jennie"",
      ""last"": ""Stokes""
    },
    ""company"": ""UNCORP"",
    ""email"": ""jennie.stokes@uncorp.com"",
    ""phone"": ""+1 (964) 534-3903"",
    ""address"": ""187 Rewe Street, Goochland, Washington, 240"",
    ""about"": ""Nulla et ea culpa consectetur elit laboris tempor id elit. Minim reprehenderit ipsum qui amet qui enim dolor exercitation exercitation fugiat officia occaecat laborum est. Voluptate mollit anim dolore sit nostrud enim qui est pariatur. Eu tempor occaecat non tempor commodo."",
    ""registered"": ""Monday, September 16, 2019 5:36 PM"",
    ""latitude"": ""61.805091"",
    ""longitude"": ""41.813868"",
    ""tags"": [
      ""ipsum"",
      ""excepteur"",
      ""Lorem"",
      ""ipsum"",
      ""dolore""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""Ward Lambert""
      },
      {
        ""id"": 1,
        ""name"": ""Rena Wilson""
      },
      {
        ""id"": 2,
        ""name"": ""Alyce Daniel""
      }
    ],
    ""greeting"": ""Hello, Jennie! You have 8 unread messages."",
    ""favoriteFruit"": ""apple""
  },
  {
    ""_id"": ""5dbc756ff3c863e47b868de0"",
    ""index"": 1,
    ""guid"": ""361551dd-7e5f-4017-a10d-5c29f7d5d7b4"",
    ""isActive"": true,
    ""balance"": ""$1,606.61"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 28,
    ""eyeColor"": ""green"",
    ""name"": {
      ""first"": ""Lula"",
      ""last"": ""Bullock""
    },
    ""company"": ""ARCTIQ"",
    ""email"": ""lula.bullock@arctiq.net"",
    ""phone"": ""+1 (851) 567-2869"",
    ""address"": ""930 Dennett Place, Hiko, Oklahoma, 5816"",
    ""about"": ""Dolore in sit deserunt laboris cillum aliquip pariatur. Quis consequat velit mollit enim cillum fugiat esse laborum dolor aute pariatur esse. Nisi mollit magna eiusmod deserunt."",
    ""registered"": ""Thursday, July 2, 2015 9:46 PM"",
    ""latitude"": ""-22.847612"",
    ""longitude"": ""115.468434"",
    ""tags"": [
      ""cupidatat"",
      ""ad"",
      ""fugiat"",
      ""officia"",
      ""ea""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""Joyner Washington""
      },
      {
        ""id"": 1,
        ""name"": ""Ballard Burch""
      },
      {
        ""id"": 2,
        ""name"": ""Gabriela Cox""
      }
    ],
    ""greeting"": ""Hello, Lula! You have 6 unread messages."",
    ""favoriteFruit"": ""banana""
  },
  {
    ""_id"": ""5dbc756f1d17dd189dcd52c6"",
    ""index"": 2,
    ""guid"": ""29b374d3-51ba-4d54-a227-57929accac9d"",
    ""isActive"": false,
    ""balance"": ""$2,423.23"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 20,
    ""eyeColor"": ""blue"",
    ""name"": {
      ""first"": ""West"",
      ""last"": ""Lynn""
    },
    ""company"": ""COMTRACT"",
    ""email"": ""west.lynn@comtract.us"",
    ""phone"": ""+1 (989) 564-2112"",
    ""address"": ""505 Brigham Street, Winfred, Puerto Rico, 5832"",
    ""about"": ""Reprehenderit culpa id dolore duis elit fugiat culpa commodo excepteur minim. Culpa ex nisi cillum id. Eu excepteur sint eu elit elit quis sit duis nostrud duis ea exercitation laborum. Mollit cillum dolor consequat cupidatat sit."",
    ""registered"": ""Sunday, August 10, 2014 4:04 PM"",
    ""latitude"": ""-44.207611"",
    ""longitude"": ""81.339187"",
    ""tags"": [
      ""consequat"",
      ""exercitation"",
      ""commodo"",
      ""Lorem"",
      ""labore""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""Jaime Burke""
      },
      {
        ""id"": 1,
        ""name"": ""Pitts Donovan""
      },
      {
        ""id"": 2,
        ""name"": ""Marian Rodgers""
      }
    ],
    ""greeting"": ""Hello, West! You have 9 unread messages."",
    ""favoriteFruit"": ""banana""
  },
  {
    ""_id"": ""5dbc756f34faa2e1a6951445"",
    ""index"": 3,
    ""guid"": ""bf05e762-fc55-4088-8302-e9af06ea6bfe"",
    ""isActive"": false,
    ""balance"": ""$3,369.63"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 25,
    ""eyeColor"": ""brown"",
    ""name"": {
      ""first"": ""Craft"",
      ""last"": ""Weiss""
    },
    ""company"": ""AVIT"",
    ""email"": ""craft.weiss@avit.io"",
    ""phone"": ""+1 (977) 531-2044"",
    ""address"": ""217 Forest Place, Loveland, Colorado, 2774"",
    ""about"": ""Enim anim quis qui dolor eiusmod nisi. Fugiat fugiat voluptate irure sint et occaecat dolore in mollit officia. Esse nisi aliqua ullamco cupidatat consectetur deserunt. Nostrud eu irure veniam in anim laboris proident sunt excepteur excepteur. Exercitation in eiusmod ex et incididunt consequat esse tempor exercitation adipisicing in exercitation."",
    ""registered"": ""Tuesday, December 4, 2018 11:18 PM"",
    ""latitude"": ""27.440023"",
    ""longitude"": ""-2.683419"",
    ""tags"": [
      ""ut"",
      ""velit"",
      ""do"",
      ""consectetur"",
      ""cillum""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""Henry Heath""
      },
      {
        ""id"": 1,
        ""name"": ""Bridges Morin""
      },
      {
        ""id"": 2,
        ""name"": ""Dalton Petersen""
      }
    ],
    ""greeting"": ""Hello, Craft! You have 7 unread messages."",
    ""favoriteFruit"": ""apple""
  },
  {
    ""_id"": ""5dbc756f7543dae187b26289"",
    ""index"": 4,
    ""guid"": ""eddf94b1-2a71-4f40-bbf4-543a6137bf17"",
    ""isActive"": true,
    ""balance"": ""$3,434.52"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 33,
    ""eyeColor"": ""green"",
    ""name"": {
      ""first"": ""Lacey"",
      ""last"": ""Delgado""
    },
    ""company"": ""NURPLEX"",
    ""email"": ""lacey.delgado@nurplex.co.uk"",
    ""phone"": ""+1 (960) 507-3122"",
    ""address"": ""217 Florence Avenue, Caledonia, Mississippi, 7227"",
    ""about"": ""Est ex Lorem occaecat esse amet. Adipisicing do incididunt quis laboris deserunt sit sint tempor. Proident aute qui id magna officia quis culpa et ipsum. Aliqua occaecat laborum sint cillum amet non excepteur incididunt ullamco velit sit. Consequat ex labore aute proident id eu elit ullamco et reprehenderit non quis amet."",
    ""registered"": ""Sunday, February 3, 2019 12:57 AM"",
    ""latitude"": ""21.932266"",
    ""longitude"": ""95.144864"",
    ""tags"": [
      ""incididunt"",
      ""pariatur"",
      ""non"",
      ""incididunt"",
      ""id""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""Kim Joyce""
      },
      {
        ""id"": 1,
        ""name"": ""Clarke Norris""
      },
      {
        ""id"": 2,
        ""name"": ""Pansy Hall""
      }
    ],
    ""greeting"": ""Hello, Lacey! You have 7 unread messages."",
    ""favoriteFruit"": ""strawberry""
  },
  {
    ""_id"": ""5dbc756faa450e09da65250e"",
    ""index"": 5,
    ""guid"": ""0bfadc8d-87a7-47e1-a4dd-857cd28ee28a"",
    ""isActive"": false,
    ""balance"": ""$1,261.36"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 21,
    ""eyeColor"": ""brown"",
    ""name"": {
      ""first"": ""Alicia"",
      ""last"": ""Sellers""
    },
    ""company"": ""COMVENE"",
    ""email"": ""alicia.sellers@comvene.biz"",
    ""phone"": ""+1 (971) 416-2770"",
    ""address"": ""132 Lawrence Street, Bergoo, Kentucky, 5708"",
    ""about"": ""Ullamco minim in fugiat ex sunt dolor aliquip. Pariatur Lorem labore fugiat magna. Aliquip incididunt sint ea cupidatat duis ut. Est dolore nostrud non ea officia irure eiusmod officia. Ut do elit elit exercitation deserunt commodo cillum do cupidatat elit aliquip minim."",
    ""registered"": ""Thursday, May 10, 2018 9:03 PM"",
    ""latitude"": ""78.975234"",
    ""longitude"": ""122.792242"",
    ""tags"": [
      ""est"",
      ""nulla"",
      ""mollit"",
      ""incididunt"",
      ""quis""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""French Jarvis""
      },
      {
        ""id"": 1,
        ""name"": ""Bobbi Anderson""
      },
      {
        ""id"": 2,
        ""name"": ""Vance Wheeler""
      }
    ],
    ""greeting"": ""Hello, Alicia! You have 5 unread messages."",
    ""favoriteFruit"": ""apple""
  },
  {
    ""_id"": ""5dbc756f1fbcc42d8e751055"",
    ""index"": 6,
    ""guid"": ""fa7bb142-4bc5-458d-98e7-fc1473240032"",
    ""isActive"": true,
    ""balance"": ""$2,074.72"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 24,
    ""eyeColor"": ""blue"",
    ""name"": {
      ""first"": ""Diana"",
      ""last"": ""Sutton""
    },
    ""company"": ""AQUACINE"",
    ""email"": ""diana.sutton@aquacine.me"",
    ""phone"": ""+1 (910) 588-3614"",
    ""address"": ""626 Graham Avenue, Maybell, New York, 5954"",
    ""about"": ""Cupidatat sit tempor Lorem consectetur eiusmod. Amet incididunt id nostrud et eu aliqua cupidatat minim commodo qui aliquip ut id. Adipisicing labore adipisicing occaecat proident nostrud."",
    ""registered"": ""Sunday, May 31, 2015 7:47 AM"",
    ""latitude"": ""-71.539475"",
    ""longitude"": ""173.163763"",
    ""tags"": [
      ""dolor"",
      ""elit"",
      ""veniam"",
      ""ut"",
      ""ea""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""Dorsey Foster""
      },
      {
        ""id"": 1,
        ""name"": ""Salas Gill""
      },
      {
        ""id"": 2,
        ""name"": ""Robbie Sandoval""
      }
    ],
    ""greeting"": ""Hello, Diana! You have 7 unread messages."",
    ""favoriteFruit"": ""apple""
  },
  {
    ""_id"": ""5dbc756fccbdc0e756d9679a"",
    ""index"": 7,
    ""guid"": ""c2e0917d-94b7-454d-8c9f-79acae3971c3"",
    ""isActive"": true,
    ""balance"": ""$1,264.52"",
    ""picture"": ""http://placehold.it/32x32"",
    ""age"": 24,
    ""eyeColor"": ""blue"",
    ""name"": {
      ""first"": ""Stephens"",
      ""last"": ""Watts""
    },
    ""company"": ""ENDIPIN"",
    ""email"": ""stephens.watts@endipin.ca"",
    ""phone"": ""+1 (971) 588-2513"",
    ""address"": ""418 Emerson Place, Floris, Massachusetts, 446"",
    ""about"": ""Laborum dolor nostrud aliquip fugiat labore. Quis pariatur sint quis irure sunt est duis tempor pariatur nulla ullamco nisi. Non in cupidatat aute enim mollit reprehenderit laboris. Sint anim excepteur commodo officia enim non esse adipisicing commodo reprehenderit sunt."",
    ""registered"": ""Monday, May 15, 2017 10:33 PM"",
    ""latitude"": ""50.149967"",
    ""longitude"": ""-140.499918"",
    ""tags"": [
      ""quis"",
      ""occaecat"",
      ""nisi"",
      ""culpa"",
      ""consectetur""
    ],
    ""range"": [
      0,
      1,
      2,
      3,
      4,
      5,
      6,
      7,
      8,
      9
    ],
    ""friends"": [
      {
        ""id"": 0,
        ""name"": ""Lizzie Flynn""
      },
      {
        ""id"": 1,
        ""name"": ""Dawson Beard""
      },
      {
        ""id"": 2,
        ""name"": ""Gladys Craft""
      }
    ],
    ""greeting"": ""Hello, Stephens! You have 7 unread messages."",
    ""favoriteFruit"": ""banana""
  }
]";

        [TestMethod]
        public void TestFullEncoding()
        {
            //var json = new JsonParser().Parse(RawJsonText);
            var json = new JsonParser().Parse(LargeExample);
            var analyze = new Analyzer();
            analyze.Test(json);
            var encoding = new Encoder().CreateEncoding(analyze);
            int compressed;
            string view;
            using (var m = new System.IO.MemoryStream())
            using (var w = new EncodedWriter(m))
            {
                w.Write(encoding);
                w.Write(json);
                w.Flush();
                var data = m.ToArray();
                view = System.Text.Encoding.UTF8.GetString(data);
                compressed = data.Length;
            }
            var raw = System.Text.Encoding.UTF8.GetByteCount(JsonParser.SingleLine.Parse(json));
            var won = raw - compressed;
            Assert.IsTrue(won > 0);
        }

        [TestMethod]
        public void TestCreateJsonTime()
        {
            new JsonParser().Parse(LargeExample);
        }

        [TestMethod]
        public void TestAnalyseAndEncoding()
        {
            var json = new JsonParser().Parse(LargeExample);
            var analyze = new Analyzer();
            analyze.Test(json);
            new Encoder().CreateEncoding(analyze);
        }

        [TestMethod]
        public void TestWritingWithoutEncoding()
        {
            var json = new JsonParser().Parse(LargeExample);
            var encoding = new JsonEncoding();
            using (var m = new System.IO.MemoryStream())
            using (var w = new EncodedWriter(m))
            {
                w.Write(encoding);
                w.Write(json);
                w.Flush();
            }
        }

        [TestMethod]
        public void TestWriteNormal()
        {
            var json = new JsonParser().Parse(LargeExample);
            JsonParser.SingleLine.Parse(json);
        }
    }
}
