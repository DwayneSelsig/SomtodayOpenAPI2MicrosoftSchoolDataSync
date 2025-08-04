# Somtoday Connect naar Microsoft School Data Sync
Open source oplossing om Microsoft Teams te kunnen gebruiken met School Data Sync met gegevens uit [Somtoday](https://www.som.today/) via Somtoday Connect. 
Create Microsoft School Data Sync CSV-files using the Somtoday Connect webservices. 

De CSV bestanden zijn nodig, omdat een directe verbinding niet mogelijk is. Somtoday biedt **geen** ondersteuning voor de OneRoster standaard: https://www.imsglobal.org/oneroster-v11-final-specification. Zodra zij dat wel doen, is deze applicatie overbodig.

![Logo](/SomtodayOpenAPI2MicrosoftSchoolDataSync/Resources/SOMSDS.ico)

## Functionaliteiten

* Gebruikt de Windows Event Viewer om de status te loggen.
* De lesgroepen van het huidige schooljaar worden opgevraagd.
* Lesgroepen zonder docent worden **niet** verwerkt.
* Lesgroepen zonder leerling worden **niet** verwerkt.
* Lesgroepen krijgen een "Uniek ID" op basis van de vestigingsafkorting. Dit ziet men **niet** terug in de DisplayName van de lesgroep.
* Ongeldige tekens worden vervangen. https://support.microsoft.com/en-us/kb/905231


## Installatie
[Download het ZIP-bestand](https://github.com/DwayneSelsig/SomtodayOpenAPI2MicrosoftSchoolDataSync/releases) en pak de bestanden uit.

Tip: Maak een scheduled task aan. Het synchroniseren van leerlingegevens is alleen ’s nachts toegestaan vanuit Somtoday.


## Configuratiestappen na installatie
Ga naar de installatie directory en bewerk SomtodayOpenAPI2MicrosoftSchoolDataSync.exe.config in een text editor.

### SomOmgeving

Kies hier om te verbinden met de testomgeving of de productieomgeving. Vul in:
* PROD
* TEST

### BooleanFilterBylocation

Filter toepassen of alle vestigingen opvragen.
* False: alle vestigingen opvragen. (aanbevolen)
* True: alleen onderstaande vestigingen opvragen.

### IncludedLocationCode

Indien BooleanFilterBylocation op True staat, kan je hier de afkortingen van de vestigingen opgeven. Puntkomma gescheiden. Niet hoofdlettergevoelig.
* Voorbeeld: AB;cd;Ef


### SchoolUUID

Het organisatie UUID. Dit kan je opvragen via https://api.somtoday.nl/rest/v1/connect/instelling
* Voorbeeld: 123e4567-e89b-12d3-a456-426614174000


### ClientId

Op te vragen door lid te worden van het Somtoday Connect Partnerprogramma op: https://som.today/somtoday-connect/contact-partnerprogramma-somtoday-connect/

### ClientSecret

Op te vragen door lid te worden van het Somtoday Connect Partnerprogramma op: https://som.today/somtoday-connect/contact-partnerprogramma-somtoday-connect/


### OutputFolder

De CSV bestanden worden opgeslagen in deze map.
* Voorbeeld: C:\SchoolDataSync\CSV


### SeperateOutputFolderForEachLocation

Maak voor elke vestiging een eigen map aan. Dit kan gebruikt worden als je meerdere synchronisatieprofielen hebt binnen School Data Sync.
* False: alle gegevens in bovenstaande OutputDirectory opslaan. (aanbevolen)
* True: maak voor elke vestiging een eigen directory aan. Dit worden subdirectories in de OutputDirectory.


### OutputFormatUsernameTeacher

* Voorbeeld: Emailadres

Mogelijk wil je een ander gegeven uit Somtoday gebruiken om gebruikers te herkennen in de Active Directory. Kijk voor de attributen op: https://editor.swagger.io/?url=https://api.somtoday.nl/rest/v1/connect/documented/openapi

### OutputFormatUsernameStudent

* Voorbeeld: Emailadres

Mogelijk wil je een ander gegeven uit Somtoday gebruiken om gebruikers te herkennen in de Active Directory. Kijk voor de attributen op: https://editor.swagger.io/?url=https://api.somtoday.nl/rest/v1/connect/documented/openapi


### EnableGuardianSync
* True: Er worden 2 extra CSV-bestanden aangemaakt met informatie over de ouders/verzorgers.
* False: Informatie over ouders/verzorgers wordt niet gesynct. (aanbevolen)

Let op! Leerlingen ouder dan 18 jaar kunnen ervoor kiezen dat ouders geen inzage hebben in hun schoolprestaties. Aangezien deze keuze niet wordt doorgegeven door Somtoday, moet de instelling voor de wekelijkse samenvatting per e-mail voor iedereen uitgeschakeld blijven. Standaard staat deze e-mail uit, zie deze link voor meer informatie:
https://docs.microsoft.com/en-us/MicrosoftTeams/expand-teams-across-your-org/assignments-in-teams#weekly-guardian-email-digest

### ClearCsvAtYearEnd
* True: Op 31 juli wordt er geen data opgehaald, maar de bestaande CSV-bestanden worden leeggemaakt.
* False: Op 31 juli kan wordt er data opgehaald, omgezet en opgeslagen in CSV-bestanden.


### SdsCsvVersion

Je kan kiezen voor CSV-bestanden in format [V1](https://aka.ms/sdsV1csv) of [V2.1](https://aka.ms/sdsV2dot1). School Data Sync accepteert beide formaten. Vul in:
* 1
* 2


## Volgende stappen

Upload de CSV-Bestanden naar School Data Sync:
https://learn.microsoft.com/en-us/schooldatasync/data-ingestion-with-sds-v2.1-csv



# Koppelen met Magister
Gebruikt jouw school Magister en zoek je een koppeling tussen Magister en School Data Sync? Bezoek dan https://github.com/sikkepitje/TeamSync
