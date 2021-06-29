using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ccgenerator;

//Aqui criaremos o nosso gRPC server.
namespace server
{
    class Program
    {
        const int Port = 50052; //Definição da porta do servidor
        static void Main(string[] args)
        {
            Server server = null; //Garantir ao inicio do método Main que o servidor esteja fechado
            try
            {
                server = new Server() //Instanciando um servidor
                {
                    Services = { CreditCardService.BindService(new CreditcardServiceImpl()) }, //Indicando e inicializando os serviços que criamos no arquivo .proto.
                    Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) } //Indicando a porta do servidor, e indicando que não há necessidade de credenciais para entrar
                };

                server.Start(); //Iniciar servidor
                Console.WriteLine("The server is listening on the port : " + Port); //Mensagem de resposta caso o servidor inicialize com sucesso
                Console.ReadKey();
                
            }
            catch (InvalidOperationException e) //Tratamento de exceção caso o servidor tenha problema para inicializar
            {
                Console.WriteLine("The server failed to connect: " + e.Message);
                Console.ReadKey();
                throw;
            }
            finally
            {
                if (server != null)
                    server.ShutdownAsync().Wait(); //Fechamento do servidor.
            }
        }
    }
}
