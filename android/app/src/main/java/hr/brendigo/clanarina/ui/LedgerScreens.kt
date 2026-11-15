package hr.brendigo.clanarina

import androidx.compose.foundation.layout.*
import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.shape.RoundedCornerShape
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
            catch (e: IllegalArgumentException) { message = e.message ?: "Provjerite iznos." }
            catch (e: Exception) { message = "Spremanje nije uspjelo. Provjerite iznos ili duplikate." }
        }) { Text("Spremi") } },
        dismissButton = { TextButton(onClick = onClose) { Text("Odustani") } }
    )
}

@Composable private fun LedgerItem(item: LedgerEntry) {
    Card(
        modifier=Modifier.fillMaxWidth(),
        shape=RoundedCornerShape(16.dp),
        border=BorderStroke(1.dp,Brand.Stroke),
        colors=CardDefaults.cardColors(containerColor=Color.White)
    ) {
        Column(
            Modifier.fillMaxWidth().padding(16.dp),
            verticalArrangement=Arrangement.spacedBy(6.dp)
        ) {
            Text(item.memberName,color=Brand.Navy,fontWeight=FontWeight.Bold,
                style=MaterialTheme.typography.titleMedium)
            Text(euroAmount(item.amountCents),color=Brand.Ink,
                fontWeight=FontWeight.Bold,style=MaterialTheme.typography.titleLarge)
            Text((if (item.kind == "charge") "Razdoblje: " else "Referenca: ") +
                item.extra.ifBlank { "Nije navedeno" },
                color=Brand.Ink,style=MaterialTheme.typography.bodyMedium)
            Text(item.createdAt,color=Brand.Muted,
                style=MaterialTheme.typography.bodySmall)
        }
    }
}
