# EGC – Documentație Temă de Casă: Camera Orbită

Sinteza cerințelor de implementare pentru camera de tip orbită și a cerințelor bonus (Secțiunea 7).

### Cerința 1: Structura clasei `OrbitCamera` și calculul matematic

* **Moștenire**: Clasa `OrbitCamera` derivă din `Camera`, preluând automat proprietățile de proiecție ($vFOV$, $hFOV$, $Aspect$, $Near$, $Far$).

* **Starea camerei**: Este definită prin **țintă** (punctul central spre care se privește), **distanță** ($R$, raza sferei de orbitare), **azimut** ($\theta$, rotația orizontală) și **elevație** ($\phi$, rotația verticală).

* **Calculul poziției**:
  

  $$
  X = X_{\text{targ}} + R \cdot \cos(\phi) \cdot \cos(\theta)
  $$

  $$
  Y = Y_{\text{targ}} + R \cdot \sin(\phi)
  $$

  $$
  Z = Z_{\text{targ}} + R \cdot \cos(\phi) \cdot \sin(\theta)
  $$

* **Matricea View**: Se generează prin apelul `Matrix4.LookAt(Position, Target, Vector3.UnitY)`.

* **Membrii abstracți**: Se implementează proprietățile obligatorii (`Name`, `Position`, `Front`, `ViewMatrix`), logica de `Update` și `Reset` (revenire la valorile implicite).

### Cerința 2: Controlul interactiv și constrângerile camerei

* **Rotire prin mouse**: Are loc exclusiv cu **clic stânga ținut apăsat**. Deplasarea mouse-ului modifică azimutul (pe axa X) și elevația (pe axa Y, cu semn inversat).

* **Rotire prin tastatură**: Tastele direcționale (Săgeți) modifică unghiurile cu o viteză constantă scalată cu timpul scurs ($dt$).

* **Zoom mecanic (Rotița mouse-ului)**: Modifică direct **distanța** $R$ față de obiect, nu unghiul de deschidere ($vFOV$). Valoarea este plafonată între limite prestabilite pentru a nu trece prin țintă.

* **Limitarea unghiului de elevație**: Elevația este constrânsă strict în intervalul $[-89^\circ, +89^\circ]$. La $\pm 90^\circ$, direcția de privire devine coliniară cu axa $Y$ a lumii (`WorldUp`), produsul vectorial se anulează, iar orientarea camerei devine instabilă (fenomen înrudit cu *gimbal lock*).

### Cerința 3: Integrarea în `Game.cs` și comutarea (Tasta V)

* **Gestiunea camerelor**: `Game` deține două instanțe (`FpsCamera` și `OrbitCamera`). Variabila `_active` indică spre camera utilizată la randare.

* **Redimensionare**: Ambele camere sunt notificate la schimbarea mărimii ferestrei (`OnResize`) pentru recalcularea aspectului.

* **Comutarea (Tasta V)**: Alternează camera activă între modurile FPS și Orbită.

* **Starea cursorului**:

  * Mod FPS $\rightarrow$ Cursor ascuns și blocat (`CursorState.Grabbed`).

  * Mod Orbită $\rightarrow$ Cursor vizibil și liber (`CursorState.Normal`).

* **Filtrare salt cursor**: La schimbarea modului se ignoră prima deplasare a mouse-ului (`_ignoreMouseOnce = true`) pentru a evita smuciturile de cadru.

### Cerința 4: Informații de stare în consolă (Tasta I)

* Suprascrierea metodei `Describe()` în `OrbitCamera` pentru a returna:

  * Coordonatele punctului țintă ($X, Y, Z$);

  * Distanța curentă față de țintă;

  * Unghiurile de azimut și elevație (în grade, normalizate în intervalul $(-180^\circ, 180^\circ]$);

  * Poziția carteziană a ochiului camerei.

* La apăsarea tastei **I**, consola listează parametrii specifici camerei curente.

### Bonus 1: Tranziție fără salt (Seamless Transition)

* **Comutare FPS** $\rightarrow$ **Orbită**:

  1. Se alege o distanță fixă de orbitare $R$ (ex. $5$ unități).

  2. Ținta se plasează pe axa de privire: $\mathbf{T} = \mathbf{P}_{\text{FPS}} + R \cdot \mathbf{Front}_{\text{FPS}}$.

  3. Se determină vectorul invers $\mathbf{O} = -\mathbf{Front}_{\text{FPS}}$.

  4. Din $\mathbf{O}$ se calculează unghiurile: $\text{elevație} = \arcsin(O_y)$, $\text{azimut} = \text{atan2}(O_z, O_x)$.

* **Comutare Orbită** $\rightarrow$ **FPS**:

  1. Poziția camerei FPS preia direct poziția camerei orbită.

  2. Unghiurile FPS se deduc din direcția $\mathbf{Front}$ a camerei orbită: $\text{pitch} = \arcsin(\text{front}_y)$, $\text{yaw} = \text{atan2}(\text{front}_z, \text{front}_x)$.

  3. Se recalculează vectorii bazei camerei FPS.

### Bonus 2: Translația țintei în plan orizontal (Pan cu WASD)

* În modul orbită, tastele $W, A, S, D$ deplasează **punctul țintă** în planul $XZ$:

  * Direcția „înainte” este derivată din azimut: $(\cos\theta, 0, \sin\theta)$.

  * Direcția „la dreapta” este perpendiculară: $(-\sin\theta, 0, \cos\theta)$.

* Deplasarea este normalizată și aplicată poziției țintei cu formula: $\mathbf{T} \leftarrow \mathbf{T} + \text{direcție} \cdot \text{viteză} \cdot dt$.

* Poziția camerei se recalculează automat în raport cu noua poziție a țintei.
