# AutomatiClone — Cahier des charges (version de travail)

> Statut : les décisions de conception sont dans les sections 1 à 7, le plan de prototypage dans la section 9.
> Les points ouverts de la section 8 ne bloquent pas le prototype A : ils seront tranchés en s'appuyant sur ce que les prototypes auront appris.

---

## 1. Vision

**Pitch :** un jeu d'automatisation à la première personne où l'usine, ce sont tes vies passées.

Un astronaute échoué sur une planète inconnue est piégé dans une boucle temporelle. Chaque vie est limitée par sa réserve d'oxygène. À sa mort, il réapparaît au point de départ, et chacune de ses vies précédentes continue d'exister sous la forme d'un **écho fantôme** qui rejoue exactement les mêmes gestes. En orchestrant ses vies successives, le joueur construit une chaîne de production de plus en plus ambitieuse pour progresser dans le monde et, à terme, briser la boucle.

**Objectif du document :** clarifier la conception, puis servir de référence précise pour développer le jeu.

**Expérience recherchée :**
- La satisfaction de l'automatisation (*Factorio*, *Satisfactory*), mais où chaque « machine » est une vie qu'on a jouée soi-même.
- Le plaisir du puzzle : planifier, observer, déboguer et optimiser ses boucles.
- L'immersion d'un FPS avec une interface quasi inexistante.

---

## 2. Boucle de jeu principale

1. Le joueur apparaît au point de départ avec une réserve d'oxygène pleine.
2. Il explore, récolte, transporte, transforme et construit, pendant que tous ses clones rejouent leurs vies en parallèle.
3. L'oxygène s'épuise et le personnage meurt.
4. La vie qui vient de se terminer devient un nouveau clone.
5. Le monde se réinitialise, sauf ce qui persiste (voir section 4), et une nouvelle vie commence.

---

## 3. Les clones (échos temporels)

### 3.1 Enregistrement et rejeu
- Chaque vie terminée devient un clone qui rejoue **exactement** les mêmes déplacements et actions, au même moment de la boucle.
- Les clones s'accumulent : la vie n° N démarre avec les clones des vies 1 à N-1.

### 3.2 Échec d'une action (« rejeu à l'aveugle »)
- Si l'action d'un clone n'est plus possible (par exemple, le minerai a déjà été ramassé), le clone **fait le geste sans effet**, **passe en rouge** et **continue sa boucle** normalement.
- Le clone ne s'adapte jamais : pas de recherche d'une cible alternative.
- **Vérification au moment du rejeu :** chaque action vérifie ses conditions au moment où elle est rejouée (cible présente, inventaire contenant le bon objet…). Si une condition manque, c'est un échec. Les échecs en cascade (par exemple, rater le minerai puis rater le dépôt au four, les mains vides) découlent naturellement de cette règle.
- **Signalement sur deux niveaux :**
  - un **flash rouge vif** à chaque échec, qui indique *quand* ça casse ;
  - une **teinte rouge légère permanente** dès le premier échec et jusqu'à la fin de la boucle, qui indique *quel* clone corriger.

### 3.3 Suppression d'un clone
- Le joueur peut supprimer n'importe quel clone pour refaire la vie correspondante.
- La suppression est **gratuite et illimitée**, mais se fait **uniquement entre deux vies** (au terminal ou dans la vue aérienne). Pendant une vie, le monde reste cohérent.
- Objectif : encourager l'expérimentation sans la punir, tout en donnant de la valeur au moment de planification.
- **Choix à la fin de chaque vie :** à la mort, le jeu demande au joueur s'il veut **garder** la vie qui vient de se terminer comme clone ou la **rejeter**.
- **Effets d'une vie rejetée ou d'un clone supprimé :** seul le fantôme disparaît. Ses constructions, ses apports aux chantiers et son exploration de la carte **restent acquis**. On peut ainsi consacrer une vie à explorer ou à construire, sans garder un clone qui gênerait les boucles suivantes.

### 3.4 Construction rejouée par un clone
- Construire est un geste vécu par le personnage pendant sa vie.
- Si un clone rejoue la construction d'un bâtiment qui existe déjà (parce qu'il a persisté), ou un apport à un chantier déjà terminé, c'est un **échec** selon la règle 3.2 : le clone passe en rouge, son geste est sans effet et il **garde l'objet en main** pour ses actions suivantes.
- Conséquence : une vie de construction gardée comme clone échouera à chaque boucle suivante. Il est donc naturel de la rejeter ou de supprimer son clone (règle 3.3) : les bâtiments restent.
- Autre conséquence : si le joueur démonte un bâtiment, les clones qui l'utilisaient échouent (et passent en rouge), selon la règle 3.2.

### 3.5 Aide et coopération
Deux formes de coopération coexistent :
- **Empilement (récolte et transformation) :** le travail se mesure en **ticks**. Chaque personnage (joueur ou clone) qui travaille sur une action apporte **1 tick par seconde**. Une action est terminée quand elle a reçu le nombre de ticks requis (par exemple 20 ticks pour extraire un minerai, 5 ticks pour en faire fondre un). Certaines machines produisent aussi des ticks seules (un four allumé apporte 1 tick par seconde), et les personnages qui y travaillent s'y ajoutent. Si plusieurs personnages travaillent sur la même action, leurs ticks se cumulent. Certaines ressources demandent plus de ticks qu'une vie ne peut en fournir seule : il faut plusieurs clones synchronisés pour les collecter. Les coûts en ticks sont regroupés dans un tableau d'équilibrage.
  - **Progression conservée :** les ticks reçus restent acquis sur l'objet pendant toute la boucle. Si un personnage s'en va (ou meurt) avant la fin, n'importe qui peut terminer l'action plus tard (par exemple, un clone mine 12 ticks sur 20 et un autre fait les 8 restants). La progression est remise à zéro à la boucle suivante, comme le reste du monde.
- **Coordination (mécanismes du décor) :** certaines actions demandent des gestes complémentaires et simultanés (tenir une plaque de pression pendant qu'un autre passe une porte, actionner un levier…). Elles servent notamment de puzzles de déblocage des zones.

### 3.6 Minage et minerai au sol
- Un point de minage produit **1 minerai** chaque fois qu'il a reçu les ticks requis. Le minerai **tombe au pied du point de minage** et la progression repart à zéro pour le minerai suivant.
- **Ramasser** est une action séparée. Elle vise le tas de minerai d'un point de minage, et non un minerai précis : n'importe quel personnage peut ramasser n'importe quel minerai du tas.
- Les minerais non ramassés **s'accumulent** au pied du point de minage, jusqu'à une **limite** (valeur d'équilibrage par point de minage).
- **Reposer :** on peut reposer une ressource au pied de son propre gisement (dans la limite). C'est le seul moyen de se libérer les mains, puisqu'on ne porte qu'un objet à la fois.
- **Blocage :** quand la limite est atteinte, les personnages qui minent ce point sont **en erreur** et n'apportent plus de ticks. Dès qu'un minerai est ramassé et qu'une place se libère, ils **sortent de l'erreur et reprennent le minage** (s'ils sont encore en train de miner dans leur rejeu).
- C'est une **attente**, différente de l'échec de la règle 3.2, et elle a sa propre couleur, l'**orange** :
  - **orange vif** pendant tout le blocage ;
  - **teinte orange légère permanente** dès le premier blocage et jusqu'à la fin de la boucle.
- Lecture pour le joueur : **rouge** = le script du clone est cassé, il faut le supprimer ou refaire la vie ; **orange** = un problème de rythme (il manque des porteurs), le clone lui-même n'a rien à changer.
- Les minerais au sol sont réinitialisés à chaque boucle, comme le reste du monde.

### 3.7 Priorité entre personnages
- Quand plusieurs personnages veulent la même ressource au même instant, ils sont servis **par ordre de vie, de la plus ancienne à la plus récente** ; le joueur (vie en cours) passe en dernier. Une copie passe juste après son original.
- Le passé est prioritaire : ajouter une vie ne peut jamais casser les précédentes par accident.
- En pratique, ce cas est rare : il faut viser le même objet exactement au même instant, ce qui n'arrive guère qu'entre un clone et ses copies. La règle garantit surtout un rejeu identique d'une boucle à l'autre.

### 3.8 Collisions
- **Aucune collision** entre le joueur et les clones, ni entre clones : on les traverse.
- Un clone ne peut jamais être dévié de sa trajectoire, ce qui garantit un rejeu exact.
- La seule manière d'interférer avec un clone passe par le monde partagé (ressources, bâtiments, mécanismes), jamais par son corps.

---

## 4. Persistance entre les boucles

| Élément | À la mort |
|---|---|
| Constructions (machines, bâtiments) | **Persistent** |
| Chantiers inachevés | **Persistent**, avec les matériaux déjà apportés |
| Ressources dans le monde | Réinitialisées (les gisements sont à nouveau récoltables) |
| Ressources transportées ou produites | Perdues, sauf si elles sont dans le coffre |
| Coffre de transfert (débloqué en cours de partie) | Son contenu **persiste** d'une boucle à l'autre |
| Zones débloquées | Restent accessibles définitivement |
| Clones | Conservés (sauf suppression par le joueur) |
| Carte explorée | Conservée (voir 6.3) |

---

## 5. Monde et progression

### 5.1 Structure
- Un **monde unique et continu**, découpé en **zones à débloquer**.
- Chaque zone a un objectif dont la réussite ouvre la ou les zones suivantes.
- Une zone débloquée reste accessible pour toujours, et ses ressources restent récoltables.
- La distance au point de départ est une contrainte centrale : plus une zone est loin, plus le trajet coûte en oxygène.

### 5.2 Progression
- **Oxygène améliorable :** la durée de vie de départ est courte et s'allonge grâce à des déblocages.
- **Transports de plus en plus rapides :** ils rendent les zones lointaines atteignables et rentables.
- **Coffre de transfert :** un palier de progression majeur, qui permet pour la première fois d'accumuler des ressources d'une vie à l'autre.
- Un point d'apparition unique (pas de points de réapparition multiples).
- **Amélioration « duplication » :** elle se débloque au cours de la partie (elle est absente du prototype A). Une fois débloquée, supprimer un clone donne l'option de le remplacer par une **copie d'un autre clone**. La copie rejoue exactement la même vie que l'original, ce qui double son travail (les ticks se cumulent, règle 3.5).
  - **Priorité de l'original :** si l'original et sa copie veulent ramasser au même moment alors qu'il ne reste qu'un minerai disponible, l'**original le prend** et la **copie passe en erreur** (règle 3.2).
  - Le joueur corrige en faisant miner plus de clones ou en améliorant le minage (voir ci-dessous), pour que plus de minerai soit disponible.
- **Amélioration du minage :** elle augmente la production d'un point de minage (par exemple 2 minerais par extraction au lieu d'1), pour que des clones dupliqués aient chacun un objet à ramasser. Le détail des paliers reste à définir.

### 5.3 Fin
- La boucle temporelle et l'échouage appellent un objectif final narratif (réparer le vaisseau, briser la boucle…). Détail à définir.

---

## 6. Interface et lisibilité

### 6.1 Principe : une interface diégétique et épurée
- Aucun HUD classique : l'information est intégrée au monde et à l'équipement.
- **Oxygène :** affiché sur la combinaison du personnage.
- **Objectifs :** consultables sur un ordinateur (son emplacement est à définir, voir 8).

### 6.2 Visibilité des clones en vue FPS
- Les clones **proches** sont visibles à travers les murs (contour lumineux), grâce au « scanner d'échos temporels » du casque. C'est une option activable ou désactivable.
- Le joueur reçoit une alerte quand un clone échoue (il passe en rouge).

### 6.3 Vue aérienne temporelle
- Accessible entre deux vies : une vue aérienne avec les trajets des clones et une **frise chronologique** à faire défiler pour voir qui fait quoi et à quel moment.
- On y gère les clones (suppression notamment).
- **Brouillard mémoriel :** la vue n'affiche **que ce qui a été exploré par les vies passées**. Parcourir une zone non explorée dans la vue aérienne ne donne aucune information. L'exploration a donc une valeur propre.

---

## 7. Direction technique

- **Moteur :** Godot 4.
- **Genre et vue :** FPS en 3D.
- **Enregistrement des clones :** on n'enregistre pas les entrées clavier et souris (trop fragile). On enregistre à chaque tick :
  - la **position et l'orientation** du personnage ;
  - des **événements discrets** horodatés (par exemple « ramasse un minerai au point de minage #3 à t=12,3 s », « construit un four en (x, y, z) », « active le levier #7 »).
  - Les événements visent un **lieu ou un conteneur** (tas de minerai, four, dépôt), jamais un objet précis : ainsi, un clone peut ramasser n'importe quel minerai disponible, ce qui est indispensable pour la duplication.
- **Rejeu :** le clone suit sa trajectoire enregistrée et tente chaque événement au bon moment. Si la cible n'existe plus ou n'est plus valide, l'action échoue et le clone passe en rouge (règle 3.2).
- **Travail en ticks :** la progression des actions (minage, fonte…) avance par ticks, à raison d'1 tick par seconde et par personnage présent. Les coûts en ticks sont des données (tableau d'équilibrage), pas des valeurs codées en dur.
- **Rendu des clones :** un shader « fantôme » (transparence, contour lumineux, rouge en cas d'échec, orange en cas d'attente).
- **Performances :** viser plusieurs dizaines de clones simultanés.

---

## 8. Hypothèses à confirmer et points ouverts

### 8.1 Interface
- [ ] Emplacement de l'ordinateur des objectifs : au poignet, sur un terminal fixe au point d'apparition, ou les deux.
- [ ] L'oxygène est-il consommé pendant qu'on consulte le terminal ou la vue aérienne ? (Suggestion : non, ce sont des actions faites dans un sas pressurisé, et la vie commence en sortant.)
- [ ] Portée exacte des clones « proches » visibles à travers les murs.

### 8.2 Clones
- [ ] Y a-t-il un nombre maximal de clones dans le jeu final ? (Piste : une limite améliorable, à calibrer avec le nombre de clones observé dans le prototype A.)
- [ ] Duplication : à quel palier de progression se débloque-t-elle exactement ? Quels paliers pour l'amélioration du minage ?
- [ ] Un clone qui rejoue un trajet en véhicule, alors que le véhicule est déjà utilisé par la vie actuelle : que se passe-t-il ?

### 8.3 Économie et contenu
- [ ] Liste des ressources, chaînes de transformation et bâtiments.
- [ ] Quantité de « travail » par type de ressource (combien de clones nécessaires).
- [ ] Moment de déblocage et capacité du coffre de transfert.
- [ ] Durée initiale de l'oxygène et paliers d'amélioration.
- [ ] Liste des transports et de leurs vitesses.

### 8.4 Monde et narration
- [ ] Nombre de zones, leurs objectifs et les mécaniques introduites dans chacune.
- [ ] Histoire : cause de la boucle, objectif final, fin.

---

## 9. Plan de prototypage

Objectif : répondre au plus vite à la question « est-ce amusant d'orchestrer ses vies passées ? ». Graphismes en formes simples (cubes, capsules).

### Prototype A — la boucle seule

**Contenu**
- Une petite carte en plein air, avec un point d'apparition, trois gisements et un point de livraison.
- Un timer d'oxygène fixe (environ 60 s).
- **Trois gisements**, minés par empilement (règle 3.5). Leurs ressources tombent au sol, doivent être ramassées, et s'accumulent au pied du gisement jusqu'à une limite qui bloque les mineurs (règle 3.6). Les gisements sont inépuisables.
  - **Fer** : le minerai à faire fondre en lingots.
  - **Pierre** : sert à construire les fours.
  - **Charbon** : le combustible des fours. **1 charbon = 10 s de fonctionnement**.
- Des fours pour faire fondre le fer, **construits par le joueur** avec de la pierre (teste la persistance des constructions et la règle 3.4) :
  - **nombre libre** : on peut construire autant de fours qu'on veut ;
  - **placement libre** au sol, à l'endroit visé, avec un espacement minimal entre bâtiments ;
  - **chantier** : poser un four crée un chantier qui reçoit les pierres une par une (la capacité de transport est d'1 objet). Le four est terminé quand le chantier a reçu tous ses matériaux. Un chantier inachevé **persiste d'une boucle à l'autre** avec ses matériaux (section 4), ce qui permet de construire sur plusieurs vies ;
  - **rejeu** : si un clone rejoue la pose d'un four déjà construit, ou un apport à un chantier déjà terminé, il passe en erreur et **garde l'objet en main** (règle 3.4). Il peut donc l'utiliser pour son action suivante, par exemple le mettre dans le four.
- **Fonctionnement du four :** on y met du fer et du charbon. Tant qu'il a du combustible, le four **fond tout seul** à 1 tick par seconde. Chaque personnage qui travaille au four **ajoute 1 tick par seconde** (règle 3.5). Un lingot sort tous les 5 ticks. Une fois allumé, le four **brûle son charbon jusqu'au bout, même à vide** : il faut synchroniser l'arrivée du fer et du charbon pour ne pas gaspiller.
- Un **établi**, construit par le joueur (même système de chantier que le four), qui donne accès à des recettes. Dans le prototype, une seule recette : l'**engrenage** (2 lingots de fer donnent 1 engrenage). L'établi ne travaille pas seul : seuls les personnages qui y travaillent apportent des ticks.
- Chaîne de production complète : **gisements → four → établi → point de livraison**.
- Un point de livraison avec un objectif : livrer 5 engrenages **au cours d'une même boucle**. Toutes les livraisons de cette boucle comptent, qu'elles viennent du joueur ou des clones.
- Le rejeu des clones, avec l'échec en rouge (règle 3.2) et l'attente en orange (règle 3.6), chacun sur deux niveaux.
- La suppression de clones par une simple touche, **à tout moment** (y compris pendant une vie). Simplification propre au prototype : le jeu final la limite à l'intervalle entre deux vies (règle 3.3).
- **Pas de limite de clones**, pour observer combien de clones les testeurs utilisent naturellement.
- Entre deux vies : un écran de transition simple (numéro de la vie, liste des clones, engrenages livrés à la boucle précédente), qui demande au joueur s'il garde ou rejette la vie qui vient de se terminer.
- Victoire : un message quand l'objectif est atteint.

**Difficulté** — l'objectif doit être impossible seul. Avec les valeurs ci-dessous, il demande environ 360 ticks de travail par boucle, soit **environ 15 à 20 clones** : le prototype teste aussi la lisibilité à grande échelle. Trois leviers combinés :
- **Volume :** une vie seule n'a pas le temps de tout faire (minage, fonte, fabrication, livraison).
- **Gros filon :** les ticks de minage se cumulent, et un minerai est calibré pour qu'il faille 2 personnages pour l'extraire dans un temps raisonnable.
- **Distance :** environ 5 s de marche entre les gisements et le four, l'établi ou le point de livraison. Les trajets restent nécessaires (chaîne de transport), sans dominer la vie.

**Valeurs de départ** (toutes réglables dans l'éditeur)

| Paramètre | Valeur de départ |
|---|---|
| Durée d'oxygène | 60 s |
| Apport de travail d'un personnage | 1 tick par seconde |
| Extraction d'une ressource (fer, pierre, charbon) | 20 ticks (20 s seul, 10 s à deux), à différencier par ressource si besoin |
| Ressources produites par extraction | 1 |
| Limite de ressources au pied d'un gisement | 10 |
| Fonte d'un minerai de fer en lingot | 5 ticks |
| Travail du four allumé | 1 tick par seconde, tout seul |
| Aide au four | +1 tick par seconde par personnage qui y travaille |
| Combustible du four | 1 charbon = 10 s de fonctionnement |
| Capacité de transport | 1 objet à la fois |
| Coût de construction du four | 3 pierres |
| Coût de construction de l'établi | 2 lingots de fer |
| Recette de l'engrenage | 2 lingots → 1 engrenage, 10 ticks |
| Distance gisements ↔ four / établi / point de livraison | environ 5 s de marche |
| Objectif | 5 engrenages livrés en une boucle (soit 10 lingots) |

**Simplifications propres au prototype**
- L'oxygène est affiché par un simple indicateur à l'écran, en attendant l'affichage sur la combinaison.
- Pas de visibilité des clones à travers les murs (la carte est en plein air).
- Exclus : vue aérienne, coffre, zones, transports, mécanismes de coordination, duplication, amélioration du minage.

### Prototype B — coordination
- Le prototype A, plus une porte à plaque de pression, pour tester l'aide complémentaire.

### Prototype C — lisibilité
- Le prototype B, plus une première version de la vue aérienne avec sa frise chronologique.
