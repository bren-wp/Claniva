package hr.brendigo.clanarina

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.unit.dp
import hr.brendigo.clanarina.data.LedgerEntry
import hr.brendigo.clanarina.data.LocalStore
import hr.brendigo.clanarina.data.Member
import hr.brendigo.clanarina.ui.Brand
import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.util.Locale

private fun euroAmount(cents: Long) =
    NumberFormat.getCurrencyInstance(Locale.forLanguageTag("hr-HR")).format(BigDecimal.valueOf(cents, 2))
private fun parseEuroAmount(input: String): Long =
    BigDecimal(input.trim().replace(",", ".")).setScale(2, RoundingMode.UNNECESSARY)
        .movePointRight(2).longValueExact()

@Composable fun MemberManagement(
    db: LocalStore, debtsOnly: Boolean, revision: Int, onChanged: () -> Unit
) {
    var search by remember { mutableStateOf("") }
    var activeOnly by remember { mutableStateOf(false) }
    var editing by remember { mutableStateOf<String?>(null) }
    var showingDetails by remember { mutableStateOf<String?>(null) }
    var adding by remember { mutableStateOf(false) }
    val members = remember(revision) { db.listMembers() }
    val selected = members.firstOrNull { it.id == editing }
    if (!debtsOnly) Button(onClick = { adding = true }) { Text("+ Novi član") }
    OutlinedTextField(search, onValueChange = { search = it },
        modifier = Modifier.fillMaxWidth(), singleLine = true,
        label = { Text("Pretraži članove i grupe") })
    Row(verticalAlignment = Alignment.CenterVertically) {
        Checkbox(checked = activeOnly, onCheckedChange = { activeOnly = it })
        Text("Samo aktivni")
    }
    val filtered = members.filter {
        (!debtsOnly || it.debtCents > 0L) && (!activeOnly || it.active) &&
            (it.name.contains(search, ignoreCase = true) || it.group.contains(search, ignoreCase = true))
    }
    if (filtered.isEmpty()) Notice("Nema članova koji odgovaraju filtru.")
    LazyColumn(verticalArrangement = Arrangement.spacedBy(7.dp)) {
        items(filtered, key = { it.id }) { member ->
            Card(colors = CardDefaults.cardColors(containerColor = Color.White)) {
                Column(Modifier.fillMaxWidth().padding(14.dp)) {
                    Text(member.name, fontWeight = FontWeight.Bold)
                    Text(member.group.ifBlank { "Bez grupe" })
                    Text("Zaduženo " + euroAmount(member.dueCents) +
                        " · Evidentirano " + euroAmount(member.paidCents))
                    Text("Dug: " + euroAmount(member.debtCents) +
                        " · Preplata: " + euroAmount(member.creditCents),
                        color = if (member.debtCents > 0) Brand.Danger else Brand.Teal)
                    Row {
                        TextButton(onClick = { showingDetails = member.id }) { Text("Detalji") }
                        TextButton(onClick = { editing = member.id }) { Text("Uredi") }
                    }
                }
            }
        }
    }
    if (adding || editing != null) {
        MemberEditor(selected,
            onClose = { editing = null; adding = false },
            onSave = { name, group, email, active ->
                if (adding) db.addMember(name, group, email)
                else if (selected != null) db.updateMember(selected.id, name, group, email, active)
                onChanged()
                editing = null
                adding = false
            })
    }
    if (showingDetails != null) {
        val member = members.firstOrNull { it.id == showingDetails }
        if (member != null) {
            val charges = remember(revision, member.id) { db.ledger("charge", member.id) }
            val payments = remember(revision, member.id) { db.ledger("payment", member.id) }
            AlertDialog(
                onDismissRequest = { showingDetails = null },
                title = { Text(member.name) },
                text = {
                    Column(Modifier.heightIn(max = 420.dp).verticalScroll(rememberScrollState())) {
                        Text("Grupa: " + member.group.ifBlank { "Bez grupe" })
                        Text("E-mail: " + member.email.ifBlank { "Nije unesen" })
                        Text("Zaduženja: " + euroAmount(member.dueCents))
                        Text("Uplate: " + euroAmount(member.paidCents))
                        Text("Dug: " + euroAmount(member.debtCents))
                        Text("Preplata: " + euroAmount(member.creditCents))
                        HorizontalDivider(Modifier.padding(vertical = 8.dp))
                        Text("Zaduženja (" + charges.size + ")", fontWeight = FontWeight.Bold)
                        charges.forEach { Text(it.extra.ifBlank { "Bez razdoblja" } + " · " + euroAmount(it.amountCents)) }
                        Spacer(Modifier.height(12.dp))
                        Text("Uplate (" + payments.size + ")", fontWeight = FontWeight.Bold)
                        payments.forEach { Text(it.extra.ifBlank { "Bez reference" } + " · " + euroAmount(it.amountCents)) }
                    }
                },
                confirmButton = { TextButton(onClick = {
                    showingDetails = null
                    editing = member.id
                }) { Text("Uredi") } },
                dismissButton = { TextButton(onClick = { showingDetails = null }) { Text("Zatvori") } }
            )
        }
    }
}

@Composable private fun MemberEditor(
    member: Member?, onClose: () -> Unit,
    onSave: (String, String, String, Boolean) -> Unit
) {
    var name by remember(member?.id) { mutableStateOf(member?.name.orEmpty()) }
    var group by remember(member?.id) { mutableStateOf(member?.group.orEmpty()) }
    var email by remember(member?.id) { mutableStateOf(member?.email.orEmpty()) }
    var active by remember(member?.id) { mutableStateOf(member?.active ?: true) }
    var message by remember(member?.id) { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onClose,
        title = { Text(if (member == null) "Novi član" else "Uredi člana") },
        text = {
            Column(Modifier.heightIn(max = 390.dp).verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(7.dp)) {
                OutlinedTextField(name, onValueChange = { name = it }, label = { Text("Ime i prezime") })
                OutlinedTextField(group, onValueChange = { group = it }, label = { Text("Grupa") })
                OutlinedTextField(email, onValueChange = { email = it }, label = { Text("E-mail") })
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Checkbox(checked = active, onCheckedChange = { active = it })
                    Text("Aktivan član")
                }
                if (message.isNotEmpty()) Text(message, color = MaterialTheme.colorScheme.error)
            }
        },
        confirmButton = { TextButton(onClick = {
            try { onSave(name, group, email, active) }
            catch (e: Exception) { message = e.message ?: "Podaci nisu spremljeni." }
        }) { Text("Spremi") } },
        dismissButton = { TextButton(onClick = onClose) { Text("Odustani") } }
    )
}

@Composable fun LedgerManagement(
    db: LocalStore, kind: String, revision: Int, onChanged: () -> Unit
) {
    var add by remember(kind) { mutableStateOf(false) }
    val members = remember(revision) { db.listMembers() }
    val ledger = remember(revision, kind) { db.ledger(kind) }
    Button(onClick = { add = true }, enabled = members.isNotEmpty()) {
        Text(if (kind == "charge") "+ Novo zaduženje" else "+ Nova uplata")
    }
    Spacer(Modifier.height(10.dp))
    Text(if (kind == "charge") "Povijest članarina" else "Povijest evidentiranih uplata",
        fontWeight = FontWeight.Bold)
    if (ledger.isEmpty()) Notice("Još nema evidentiranih stavki.")
    LazyColumn(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        items(ledger, key = { it.id }) { entry -> LedgerItem(entry) }
    }
    if (add) LedgerEditor(
        members, kind,
        onClose = { add = false },
        onSave = { id, amount, extra ->
            val cents = parseEuroAmount(amount)
            if (kind == "charge") db.addCharge(id, cents, extra)
            else db.addPayment(id, cents, extra)
            onChanged()
            add = false
        }
    )
}

@Composable private fun LedgerEditor(
    members: List<Member>, kind: String, onClose: () -> Unit,
    onSave: (String, String, String) -> Unit
) {
    var selectedId by remember(kind) { mutableStateOf(members.firstOrNull()?.id.orEmpty()) }
    var amount by remember(kind) { mutableStateOf("") }
    var extra by remember(kind) { mutableStateOf("") }
    var message by remember(kind) { mutableStateOf("") }
    AlertDialog(
        onDismissRequest = onClose,
        title = { Text(if (kind == "charge") "Novo zaduženje" else "Nova uplata") },
        text = {
            Column(Modifier.heightIn(max = 430.dp).verticalScroll(rememberScrollState()),
                verticalArrangement = Arrangement.spacedBy(8.dp)) {
                Text("Odaberite člana", fontWeight = FontWeight.SemiBold)
                members.forEach { member ->
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        RadioButton(selected = selectedId == member.id, onClick = { selectedId = member.id })
                        Text(member.name)
                    }
                }
                OutlinedTextField(amount, onValueChange = { amount = it },
                    label = { Text("Iznos (€)") },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal))
                OutlinedTextField(extra, onValueChange = { extra = it },
                    label = { Text(if (kind == "charge") "Razdoblje (GGGG-MM)" else "Referenca (neobavezno)") })
                if (message.isNotEmpty()) Text(message, color = MaterialTheme.colorScheme.error)
            }
        },
        confirmButton = { TextButton(onClick = {
            try { onSave(selectedId, amount, extra) }
            catch (e: Exception) { message = e.message ?: "Neuspjelo spremanje. Provjerite iznos i duplikate." }
        }) { Text("Spremi") } },
        dismissButton = { TextButton(onClick = onClose) { Text("Odustani") } }
    )
}

@Composable private fun LedgerItem(item: LedgerEntry) {
    Card(colors = CardDefaults.cardColors(containerColor = Color.White)) {
        Column(Modifier.fillMaxWidth().padding(15.dp)) {
            Text(item.memberName, fontWeight = FontWeight.Bold)
            Text(euroAmount(item.amountCents), style = MaterialTheme.typography.titleMedium)
            Text((if (item.kind == "charge") "Razdoblje: " else "Referenca: ") +
                item.extra.ifBlank { "Nije navedeno" })
            Text(item.createdAt, style = MaterialTheme.typography.bodySmall)
        }
    }
}
@Composable private fun Notice(text: String) {
    Card(colors = CardDefaults.cardColors(containerColor = Color.White)) {
        Text(text, Modifier.fillMaxWidth().padding(22.dp))
    }
}
