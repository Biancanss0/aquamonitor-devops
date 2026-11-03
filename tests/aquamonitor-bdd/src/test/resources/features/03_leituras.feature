# language: pt
@leituras
Funcionalidade: Registro de leituras e detecção automática de desperdício de água
  Como sistema de monitoramento hídrico
  Quero registrar as leituras dos sensores e identificar vazamentos e consumo excessivo
  Para reduzir o desperdício de água e agir rápido na preservação do recurso natural

  # ESG (E - Ambiental): esta é a regra central do AquaMonitor.
  #   - pressão abaixo de 0,5 bar com consumo  → vazamento  → alerta CRITICO
  #   - consumo acima do limite do sensor      → alerta MEDIO
  #   - consumo acima de 150% do limite        → alerta ALTO

  Contexto:
    Dado que existe um sensor "Residencial" cadastrado com limite de 500 litros

  @positivo @contrato
  Cenário: Registrar uma leitura dentro do limite de consumo
    Quando eu registro uma leitura de 120 litros com pressão de 3.2 bar e temperatura de 24 graus
    Então o status da resposta deve ser 201
    E a resposta deve respeitar o contrato "leitura"
    E a leitura não deve indicar vazamento
    E o cabeçalho "Location" deve estar presente

  @positivo @esg-agua
  Cenário: Leitura normal não gera alertas desnecessários
    Quando eu registro uma leitura de 300 litros com pressão de 2.8 bar e temperatura de 23 graus
    Então o status da resposta deve ser 201
    E a quantidade de alertas automáticos do sensor deve ser 0

  @positivo @contrato @esg-agua
  Esquema do Cenário: Detecção automática de anomalias gera alerta para a equipe - <situacao>
    Quando eu registro uma leitura de <consumo> litros com pressão de <pressao> bar e temperatura de 22 graus
    Então o status da resposta deve ser 201
    E a resposta deve respeitar o contrato "leitura"
    E o indicador de vazamento da leitura deve ser "<vazamento>"
    E a quantidade de alertas automáticos do sensor deve ser <alertas>
    E deve existir um alerta do tipo "<tipo>" com nível "<nivel>"

    Exemplos:
      | situacao                           | consumo | pressao | vazamento | alertas | tipo             | nivel   |
      | consumo 20% acima do limite        | 600     | 3.0     | não       | 1       | ConsumoExcessivo | MEDIO   |
      | consumo 80% acima do limite        | 900     | 3.0     | não       | 1       | ConsumoExcessivo | ALTO    |
      | queda de pressão (vazamento)       | 200     | 0.3     | sim       | 1       | Vazamento        | CRITICO |
      | vazamento com consumo excessivo    | 900     | 0.3     | sim       | 2       | Vazamento        | CRITICO |

  @negativo @contrato
  Cenário: Registrar leitura para um sensor inexistente
    Quando eu registro uma leitura de 100 litros para o sensor de identificador 999999
    Então o status da resposta deve ser 404
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Sensor 999999 não encontrado."

  @negativo @contrato @esg-governanca
  Cenário: Sensor desativado não aceita novas leituras
    Dado que o sensor foi desativado
    Quando eu registro uma leitura de 100 litros com pressão de 3.0 bar e temperatura de 22 graus
    Então o status da resposta deve ser 400
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Não é possível registrar leitura em sensor inativo."

  @negativo @contrato @esg-governanca
  Esquema do Cenário: Leitura com valor fisicamente impossível é rejeitada - <motivo>
    Quando eu registro uma leitura com o campo "<campo>" igual a "<valor>"
    Então o status da resposta deve ser 400
    E a resposta deve respeitar o contrato "erro-validacao"
    E a resposta deve apontar erro de validação no campo "<campo>"

    Exemplos:
      | motivo                     | campo              | valor |
      | consumo zerado             | ConsumoLitros      | 0     |
      | consumo negativo           | ConsumoLitros      | -50   |
      | pressão acima de 100 bar   | PressaoBar         | 150   |
      | temperatura acima de 150 C | TemperaturaCelsius | 200   |

  @positivo @contrato
  Cenário: Consultar uma leitura pelo identificador
    Dado que o sensor possui uma leitura registrada de 250 litros
    Quando eu consulto a leitura pelo seu identificador
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "leitura"
    E o campo "ConsumoLitros" da resposta deve ser 250

  @negativo @contrato
  Cenário: Consultar uma leitura inexistente
    Quando eu consulto a leitura de identificador 999999
    Então o status da resposta deve ser 404
    E a resposta deve respeitar o contrato "erro-mensagem"
    E a mensagem de erro deve ser "Leitura 999999 não encontrada."

  @positivo @contrato @esg-agua
  Cenário: Filtrar somente as leituras com vazamento de um sensor
    Dado que o sensor possui uma leitura registrada de 250 litros
    E que o sensor possui uma leitura com vazamento registrada
    Quando eu listo as leituras do sensor filtrando apenas vazamentos
    Então o status da resposta deve ser 200
    E a resposta deve respeitar o contrato "leituras-paginado"
    E o total de registros deve ser 1
    E todos os registros devem ter o campo "VazamentoDetectado" igual a "true"

  @positivo
  Cenário: Excluir uma leitura registrada por engano
    Dado que o sensor possui uma leitura registrada de 250 litros
    Quando eu excluo a leitura
    Então o status da resposta deve ser 204
    E a leitura não deve mais ser encontrada

  @negativo
  Cenário: Excluir uma leitura inexistente
    Quando eu excluo a leitura de identificador 999999
    Então o status da resposta deve ser 404
    E a mensagem de erro deve ser "Leitura 999999 não encontrada."
