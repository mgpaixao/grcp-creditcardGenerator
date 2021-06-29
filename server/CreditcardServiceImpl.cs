//Importar
using Ccgenerator;
using Grpc.Core;
using MongoDB.Bson;
using MongoDB.Driver;
using static Ccgenerator.CreditCardService;
//------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Esse arquivo, é uma classe de IMPLEMENTAÇÃO (IMPL) das funções do arquivo .proto
namespace server
{
    public class CreditcardServiceImpl : CreditCardServiceBase
    {
        //Criação do banco de dados MONGODB
        private static MongoClient mongoClient = new MongoClient("mongodb://localhost:27017"); //Definindo local e porta do cliente mongoDB. 
        private static IMongoDatabase mongoDatabase = mongoClient.GetDatabase("creditcard"); //Criando uma base de dados
        private static IMongoCollection<BsonDocument> mongoCollection = mongoDatabase.GetCollection<BsonDocument>("card");//Criando uma tabela de dados (collection)
        public override Task<CreateCCResponse> CreateCC(CreditCards request, ServerCallContext context)//Impl de criação do numéro aleatório de cartão de crédito
        {

            var card = new CreditCards(); //Variavel que instancia a função CreditCard
            bool check = new Boolean();  //Variavel que armazena o resultado da verificação de duplicidade do numero do cartão de crédito no Banco de dados.
            Random cc = new Random();   //Instacia varavel da biblioteca Random, que gera números aleatórios.
            do
            {  //Estrutura de repetição que vai gerar o número aleatório do cartão de crédito e verificar duplicidade do mesmo no banco de dados MongoDB
                var cc1 = cc.Next(1000, 9999);//O número de cartão de crédito é dividido em 4 partes, geramos então as 4 partes separadamente, podendo limitar o range 
                var cc2 = cc.Next(1000, 9999);// do número criado, nesse caso é de 1000 a 9999.
                var cc3 = cc.Next(1000, 9999);
                var cc4 = cc.Next(1000, 9999);
                card.CreditCardNumber = $"{cc1} {cc2} {cc3} {cc4}"; // Concatenamos as 4 partes e armazenamos na variavel gRPC

                var filter = new FilterDefinitionBuilder<BsonDocument>().Eq("cc", new BsonString(card.CreditCardNumber)); //Criar um filtro para busca no banco de dados, o parametro do filtro é o numero gerado de cartão de crédito
                var result = mongoCollection.Find(filter);//Fazendo a busca no banco de dados com o filtro criado

                foreach (var item in result.ToList()) //Verificando o resultado da busca, caso encontrado duplicidade volta-se ao inciio do loop e cria-se outro número.
                {
                    {
                        card = new CreditCards()
                        {
                            CreditCardNumber = item.GetValue("cc").AsString //Pegando os valores do cartão de crédito do banco de dados

                        };
                        check = true;//Caso haja duplicidade, eu uso essa variavel booleana para marcar true quando há duplicidade.
                    }
                }
            } while (check); //Verificando o estado da variavel check, se for true, repete o loop, caso for false, sai do loop.
            BsonDocument doc = new BsonDocument("email", request.Email) //Criando uma Bson document que representa a linha a ser adicionada na nossa tabela do bando de dados.
                                               .Add("cc", card.CreditCardNumber);//Adicionando o nome da coluna entre aspas e depois o valor que vai ser alimentado nessa coluna.
            mongoCollection.InsertOne(doc); //Inserindo o Bson Document no banco de dados.

            return Task.FromResult(new CreateCCResponse()
            {
                Cardresponse = card.CreditCardNumber //Retorna o numero de cartão de crédito gerado
            });

        }

        /*public override Task<ListOneCCResponse> ListOneCC()
        {
            //var email = request.Email;
            //var filter = new FilterDefinitionBuilder<BsonDocument>().AnyEq();
            var result = mongoCollection.Find(new BsonDocument()).ToList();

            foreach(BsonDocument doc in result)
            {
                Console.WriteLine(doc.ToString());
            }

           if (result == null)
                throw new RpcException(new Status(StatusCode.NotFound, "Email not found"));

            Ccgenerator.CreditCards cc = new Ccgenerator.CreditCards()
            {
                Cc = result.GetValue("cc").AsString
            };
            return new ListOneCCResponse() { List = cc };
           
        }*///Exemplo de busca por um valor apenas, e não uma lista.

        public override async Task ListCC(ListCreditCardsRequest request, IServerStreamWriter<ListCreditCardsResponse> responseStream, ServerCallContext context)
        { //Impl de busca de uma lista filtrada pelo email do usuário, no banco de dados.

            var filter = new FilterDefinitionBuilder<BsonDocument>().Eq("email", new BsonString(request.Email));//Criar um filtro para busca no banco de dados, o parametro do filtro é o numero gerado de cartão de crédito
            var result = mongoCollection.Find(filter);//Fazendo a busca no banco de dados com o filtro criado

           
            foreach (var item in result.ToList()) //Estrutura de repetição que verifica se há uma ocorrencia na lista do banco de dados
            {

                await responseStream.WriteAsync(new ListCreditCardsResponse() //Caso haja uma ocorrencia, inicia a stream para receber os dados encontrados
                {

                    List = new CreditCards()
                    {
                        CreditCardNumber = item.GetValue("cc").AsString

                    }

                });

            }
            
        }


        public override async Task ListCheck(ListCheckRequest request, IServerStreamWriter<ListCheckResponse> responseStream, ServerCallContext context)
        {//Função ListCheck verifica se o cartão de crédito gerado já existe no banco de dados

            var filter = new FilterDefinitionBuilder<BsonDocument>();
            var result = mongoCollection.Find(new BsonDocument()).ToList();

            if (result == null)
                throw new RpcException(new Status(StatusCode.NotFound, "The email was no found"));
            foreach (var item in result)
            {
                await responseStream.WriteAsync(new ListCheckResponse()
                {
                    List = new CreditCards()
                    {
                        CreditCardNumber = item.GetValue("cc").AsString
                    }
                });
            }
        }
    }
}
