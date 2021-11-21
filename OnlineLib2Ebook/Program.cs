﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using OnlineLib2Ebook.Configs;
using OnlineLib2Ebook.Logic.Builders;
using OnlineLib2Ebook.Logic.Getters;

namespace OnlineLib2Ebook {
    class Program {
        private static async Task Main(string[] args) {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(async options => {
                    var handler = new HttpClientHandler {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                    };
            
                    if (!string.IsNullOrEmpty(options.Proxy)) {
                        var split = options.Proxy.Split(":");
                        handler.Proxy = new WebProxy(split[0], int.Parse(split[1])); 
                    }

                    var client = new HttpClient(handler);
                    var url = new Uri(options.Url);
                    
                    var getterConfig = new BookGetterConfig(options, client);
                    using var getter = GetGetter(getterConfig, url);

                    var book = await getter.Get(url);
                    book.Save(GetBuilder(options.Format), options.SavePath, "Patterns");
                });
        }

        private static BuilderBase GetBuilder(string format) {
            return format switch {
                "fb2" => Fb2Builder.Create(),
                "epub" => EpubBuilder.Create(File.ReadAllText("Patterns/ChapterPattern.xhtml")),
                _ => throw new ArgumentException("Неизвестный формат", nameof(format))
            };
        }
        
        private static GetterBase GetGetter(BookGetterConfig config, Uri url) {
            var getters = new GetterBase[] {
                new LitnetGetter(config), 
                new AuthorTodayGetter(config), 
                new LitmarketGetter(config), 
                new ReadliGetter(config),
                new RulateGetter(config),
                new RanobeGetter(config),
            };

            foreach (var getter in getters) {
                if (getter.IsSameUrl(url)) {
                    return getter;
                }
            }

            throw new ArgumentException("Данная система не поддерживается", nameof(url));
        }
    }
}
