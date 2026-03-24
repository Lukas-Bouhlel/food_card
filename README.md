## 🚀 Objectif du projet 
Offrir une expérience utilisateur fluide et intuitive pour explorer une carte de plats en 3D. L'application mise sur des **gestes naturels** et une interface épurée.

## ✨ Fonctionnalités principales

### 📍 Placement AR Intelligent
* **Détection de surfaces :** Identification automatique des surfaces planes (tables, sols) via `ARFoundation`.
* **Interaction simple :** * Un **tap** pour poser le plat.
    * Un **second tap** pour le déplacer.
* **Orientation auto :** Le plat s'oriente automatiquement face à la caméra lors du premier placement.

### 🍽️ Navigation interactive
* **Gestes de Swipe :** Glissez vers la gauche ou la droite pour changer de plat.
* **Boucle circulaire :** La navigation revient automatiquement au premier plat après le dernier.
* **Contrôles hybrides :** Compatible avec les gestes tactiles et l'interface UI (`ShowNext()` / `ShowPrevious()`).

### 🔄 Manipulation & Réalisme
* **Rotation intuitive :** Faites pivoter le plat sur son axe vertical (Y) avec le doigt.
* **Zone morte :** Système de filtrage pour éviter les micro-rotations involontaires.
* **Mise à l'échelle réelle :** Chaque plat est automatiquement redimensionné en mètres pour correspondre à sa taille réelle en cuisine.

---

## 🛠️ Stack Technique
* **Moteur :** Unity (C#)
* **Framework AR :** ARFoundation (AR Raycast, Plane Tracking)
* **Sous-systèmes :** ARSubsystems
* **Compatibilité :** Cross-platform (iOS & Android)

---

## 👥 L'Équipe (Répartition des tâches)

* **Gestion d’équipe :** Lukas Bouhlel
* **Développement Logique AR :** Lukas Bouhlel & Matthieu Vernier
* **Design UI & Maquettes :** Léa Régoudis
* **Implémentation UI & Navigation :** Jun Zhi
* **QA & Tests Fonctionnels :** Vincent Altmann

---

## 📸 Aperçu du fonctionnement
1. **Scan :** Dirigez le téléphone vers une surface plane.
2. **Placement :** Appuyez pour faire apparaître votre plat.
3. **Exploration :** Swipez pour changer de menu, tournez pour voir les détails
