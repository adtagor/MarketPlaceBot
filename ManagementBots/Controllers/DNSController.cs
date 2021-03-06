﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ManagementBots.Db;
using Microsoft.EntityFrameworkCore;
using ManagementBots.BusinessLayer;
using System.IO;

namespace ManagementBots.Controllers
{
    public class DNSController : Controller
    {
        BotMngmntDbContext DbContext { get; set; }

        SshFunction ssh { get; set; }

        [HttpGet]
        public IActionResult Index()
        {
            DbContext = new BotMngmntDbContext();

            var list = DbContext.Dns.ToList();

            DbContext.Dispose();

            return View(list);
        }

        [HttpPost]
        public IActionResult Post ([FromBody] Dns dns)
        {
            try
            {
                DbContext = new BotMngmntDbContext();

                if (dns != null && dns.Name != null && dns.Name != "" && dns.Ip != "" && dns.Id > 0)
                {
                    DbUpdateDns(dns);
                    return Json("Сохранено");
                }

                var reapet = DbContext.Dns.Where(d => d.Name == dns.Name).FirstOrDefault();

                if (dns.Id == 0 && reapet != null)
                    return Json("Домен с таким именем уже существует");

                if (dns != null && dns.Name != null && dns.Name != "" && dns.Ip != "" && dns.Id == 0)
                {
                    CreateDNS(dns);

                    return Json("Добавлено");
                }

                else
                    return Json("Ошибка");
            }

            catch (Exception e)
            {
                return Json(e.Message);
            }

            finally
            {
                DbContext.Dispose();
            }
        }



        [HttpGet]
        public IActionResult Delete (int Id)
        {
            DbContext = new BotMngmntDbContext();

            try
            {
                var dns = DbContext.Dns.Find(Id);

                DbContext.Remove<Dns>(dns);

                DbContext.SaveChanges();

                return Json("Удалено");
            }

            catch (Exception e)
            {
                return Json(e.Message);
            }

            finally
            {
                DbContext.Dispose();
            }
        }

        [HttpGet]
        public IActionResult NewSsl(int DomainId)
        {
            try
            {
                return Json(SSL.GenerateSSL("C:\\MYcert", "ya.ru"));
            }

            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        private Dns CreateDNS(Dns dns)
        {

            //генерируем сертификат
            BusinessLayer.SSL.GenerateSSL(GeneralFunction.SslPathOnMainServer(), dns.Name);

            //копируем файлы сертификата на прокси сервера
            var ProxyList = DbContext.ProxyServer.Where(p => p.Enable).ToList();

            foreach (var proxy in ProxyList)
            {
                ssh = new SshFunction(proxy.Ip, proxy.CertPath, proxy.UserName, proxy.PassPhrase);

                ssh.SftpConnectToServer();

                ssh.SCPFile(OpenFile(dns.PublicKeyPathOnMainServer()), dns.PublicKeyPathOnProxy());

                ssh.SCPFile(OpenFile(dns.PrivateKeyPathOnMainServer()), dns.PrivateKeyPathOnProxy());

                ssh.Disconnect();
            }

            dns.SslPathOnProxy = GeneralFunction.SslPathOnProxyServer();
            dns.SslPathOnMainServer = GeneralFunction.SslPathOnMainServer();

            var HttpPortList = DbContext.WebHookPort.Where(p => p.Enable).ToList();

            dns=DbInsertDns(dns);

            foreach (var port in HttpPortList)
            {
                WebHookUrl hookUrl = new WebHookUrl { DnsId = dns.Id, IsFree = true, PortId = port.Id , Controller="bot"};
                DbContext.WebHookUrl.Add(hookUrl);
                DbContext.SaveChanges();
            }

            return dns;

        }

        private Dns DbInsertDns(Dns dns)
        {
            dns.TimeStamp = DateTime.Now;

            
            DbContext.Add(dns);

            DbContext.SaveChanges();

            return dns;
        }

        private Dns DbUpdateDns(Dns _dns)
        {
            var dns = DbContext.Dns.Find(_dns.Id);

            dns.Name = _dns.Name;
            dns.Enable = _dns.Enable;
            dns.Ip = _dns.Ip;
            
            DbContext.Update<Dns>(dns);

            DbContext.SaveChanges();

            return dns;
        }

        private Stream OpenFile(string Path)
        {
            FileStream fstream = System.IO.File.OpenRead(Path);

            return fstream;


        }


    }
}
