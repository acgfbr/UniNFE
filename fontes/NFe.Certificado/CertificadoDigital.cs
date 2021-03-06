﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;
using NFe.Settings;
using NFe.Components;
using NFe.Exceptions;
using System.Security;
using Microsoft.Win32;

namespace NFe.Certificado
{
    /// <summary>
    /// Classe para trabalhar com certificados digitais
    /// </summary>
    public class CertificadoDigital
    {
        #region Propriedades da classe

        /// <summary>
        /// Certificado selecionado pelo método SelecionarCertificado()
        /// </summary>
        public X509Certificate2 oCertificado { get; private set; }
        /// <summary>
        /// Data inicial da validade do certificado
        /// </summary>
        public DateTime dValidadeInicial { get; private set; }
        /// <summary>
        /// Data final da validade do certificado
        /// </summary>
        public DateTime dValidadeFinal { get; private set; }
        /// <summary>
        /// Subject do Certificado, Razão Social da Empresa Certificada, CNPJ, etc...
        /// </summary>
        public string sSubject { get; private set; }

        #endregion

        /// <summary>
        /// Método responsável por abrir um browse para selecionar o 
        /// certificado digital que será utilizado para autenticação
        /// dos WebServices e gravar ele no atributo oCertificado
        /// </summary>
        /// <returns>
        /// Retorna se o certificado foi selecionado corretamente ou não.
        /// true  = foi selecionado corretamente.
        /// false = não foi selecionado, algum problema ocorreu ou foi cancelado o selecionamento pelo usuário.
        /// </returns>
        /// <by>Wandrey Mundin Ferreira</by>
        /// <date>04/06/2008</date>
        public bool SelecionarCertificado()
        {
            bool vRetorna;

            X509Certificate2 oX509Cert = new X509Certificate2();
            X509Store store = new X509Store("MY", StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
            X509Certificate2Collection collection1 = (X509Certificate2Collection)collection.Find(X509FindType.FindByTimeValid, DateTime.Now, false);
            X509Certificate2Collection collection2 = (X509Certificate2Collection)collection.Find(X509FindType.FindByKeyUsage, X509KeyUsageFlags.DigitalSignature, false);
            X509Certificate2Collection scollection = X509Certificate2UI.SelectFromCollection(collection2, "Certificado(s) Digital(is) disponível(is)", "Selecione o certificado digital para uso no aplicativo", X509SelectionFlag.SingleSelection);

            if (scollection.Count == 0)
            {
                string msgResultado = "Nenhum certificado digital foi selecionado ou o certificado selecionado está com problemas.";
                MessageBox.Show(msgResultado, "Advertência", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                vRetorna = false;
            }
            else
            {
                oX509Cert = scollection[0];
                oCertificado = oX509Cert;
                vRetorna = true;
            }

            return vRetorna;
        }

        /// <summary>
        /// Exibi uma tela com o certificado digital selecionado para ser
        /// utilizado na integração com os WEBServices da NFe
        /// </summary>
        /// <by>Wandrey Mundin Ferreira</by>
        /// <date>04/06/2008</date>
        public void ExibirCertSel()
        {
            if (this.oCertificado == null)
            {
                MessageBox.Show("Nenhum certificado foi selecionado.", "Advertência", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                X509Certificate2UI.DisplayCertificate(oCertificado);
            }
        }

        /// <summary>
        /// Pega algumas informações do certificado digital informado por parâmetro para o método e disponibiliza em propriedades para utilização
        /// </summary>
        /// <param name="empresa">Objeto com os dados da empresa</param>
        /// <returns>Retorna se localizou o certificado ou não (True or False)</returns>
        public bool PrepInfCertificado(Empresa empresa)
        {
            bool localizouCertificado;
            X509Certificate2 x509Cert = empresa.BuscaConfiguracaoCertificado();

            if (x509Cert == null)
                localizouCertificado = false;
            else
            {
                sSubject = x509Cert.Subject;
                dValidadeInicial = x509Cert.NotBefore;
                dValidadeFinal = x509Cert.NotAfter;
                localizouCertificado = true;
            }

            return localizouCertificado;
        }

        /// <summary>
        /// Certificado digita está vencido ou não
        /// </summary>
        /// <param name="emp">Empresa que é para ser verificado o certificado</param>
        /// <returns>true = Certificado Vencido</returns>
        public bool Vencido(int emp)
        {
            bool retorna = false;

            if (Empresas.Configuracoes[emp].UsaCertificado)
            {
                if (PrepInfCertificado(Empresas.Configuracoes[emp]))
                {
                    if (DateTime.Compare(DateTime.Now, dValidadeFinal) > 0)
                    {
                        retorna = true;
                    }
                }
            }

            return retorna;
        }

        /// <summary>
        /// Lista os Providers que podem ser usados para o Certificado Digital A3
        /// </summary>
        /// <returns>Lista de Providers para o Certificado Digital</returns>
        /// <author>Renan Borges</author>
        public List<CertProviders> GetListProviders()
        {
            List<CertProviders> providers = new List<CertProviders>();

            string registry_key = @"SOFTWARE\Microsoft\Cryptography\Defaults\Provider";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        string type = subkey.GetValue("Type").ToString();
                        providers.Add(new CertProviders() { NameKey = subkey_name, Type = type });
                    }
                }
            }

            return providers;
        }

        /// <summary>
        /// Retorna os dados do provedores do certificado digital A3
        /// </summary>
        /// <param name="provider">Nome do provider</param>
        /// <returns>Objeto com dados do provider</returns>
        /// <author>Renan Borges</author>
        public CertProviders GetInfoProvider(string provider)
        {
            CertProviders oDadosCert = new CertProviders();
            oDadosCert.NameKey = provider;

            string registry_key = @"SOFTWARE\Microsoft\Cryptography\Defaults\Provider";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    if (subkey_name.Equals(oDadosCert.NameKey))
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            oDadosCert.Type = subkey.GetValue("Type").ToString();
                            break;
                        }
                    }
                }
            }

            return oDadosCert;
        }
    }

    public class CertProviders
    {
        public string NameKey { get; set; }
        public string Type { get; set; }
    }
}

namespace NFe.Exceptions
{
    /// <summary>
    /// Classe para tratamento de exceções da classe Invocar Objeto
    /// </summary>
    public class ExceptionCertificadoDigital : Exception
    {
        public ErroPadrao ErrorCode { get; private set; }

        /// <summary>
        /// Construtor que já define uma mensagem pré-definida de exceção
        /// </summary>
        /// <param name="CodigoErro">Código da mensagem de erro (Classe MsgErro)</param>
        /// <by>Wandrey Mundin Ferreira</by>
        /// <date>24/11/2009</date>
        public ExceptionCertificadoDigital(ErroPadrao Erro)
            : base(MsgErro.ErroPreDefinido(Erro))
        {
            this.ErrorCode = Erro;
        }

        /// <summary>
        /// Construtor que ´já define uma mensagem pré-definida de exceção com possibilidade de complemento da mensagem
        /// </summary>
        /// <param name="CodigoErro">Código da mensagem de erro (Classe MsgErro)</param>
        /// <param name="ComplementoMensagem">Complemento da mensagem de exceção</param>
        public ExceptionCertificadoDigital(ErroPadrao Erro, string ComplementoMensagem)
            : base(MsgErro.ErroPreDefinido(Erro, ComplementoMensagem))
        {
            this.ErrorCode = Erro;
        }
    }
}