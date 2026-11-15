package hr.brendigo.clanarina

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.*
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.res.painterResource
import hr.brendigo.clanarina.data.LocalStore
import hr.brendigo.clanarina.data.Member
import hr.brendigo.clanarina.ui.Brand
import hr.brendigo.clanarina.ui.ClanarinaTheme
import java.math.BigDecimal
import java.math.RoundingMode
import java.text.NumberFormat
import java.util.Locale

private val sections = listOf("Početna","Članovi","Članarine","Uplate","Dugovanja","Uplatnice","Bankovni izvodi","Izvještaji","Licenca","Postavke")
private val euros = NumberFormat.getCurrencyInstance(Locale.forLanguageTag("hr-HR"))
private fun euro(cents:Long) = euros.format(BigDecimal.valueOf(cents,2))
private fun parseEuro(value:String): Long = BigDecimal(value.trim().replace(",","."))
    .setScale(2,RoundingMode.UNNECESSARY).movePointRight(2).longValueExact()

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val startup = mutableStateOf(openLocalStore())
        setContent {
            ClanarinaTheme {
                val db = startup.value
                if (db == null) {
                    DatabaseRecovery(onRetry = { startup.value = openLocalStore() })
                } else {
                    MainApp(db)
                }
            }
        }
    }

    private fun openLocalStore(): LocalStore? {
        return try {
            LocalStore(applicationContext).also { it.writableDatabase }
        } catch (error: Exception) {
            android.util.Log.e("ClanarinaStartup", "Local database initialization failed", error)
            null
        }
    }
}

@Composable
private fun DatabaseRecovery(onRetry: () -> Unit) {
    Column(
        modifier = Modifier.fillMaxSize().padding(24.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text("Članarina — poteškoća s lokalnom bazom",
            style = MaterialTheme.typography.headlineSmall)
        Spacer(Modifier.height(12.dp))
        Text("Aplikacija ne može otvoriti lokalne podatke. Nijedan zapis nije namjerno obrisan. " +
             "Provjerite slobodan prostor i ponovno pokušajte. Za tehničku dijagnostiku potreban je zapis ClanarinaStartup iz logcata.")
        Spacer(Modifier.height(18.dp))
        Button(onClick = onRetry) { Text("Pokušaj ponovno") }
    }
}
@OptIn(ExperimentalMaterial3Api::class)
@Composable private fun MainApp(db:LocalStore) {
    var page by remember { mutableStateOf("Početna") }
    var menu by remember { mutableStateOf(false) }
    var form by remember { mutableStateOf("") }
    var changes by remember { mutableIntStateOf(0) }
    var error by remember { mutableStateOf("") }
    val members=remember(changes) { db.listMembers() }
    val totals=remember(changes) { db.totals() }
    Scaffold(
        topBar={ TopAppBar(
            title={
                Row(verticalAlignment=Alignment.CenterVertically,horizontalArrangement=Arrangement.spacedBy(10.dp)) {
                    Image(painter=painterResource(id=R.drawable.brand_foreground),contentDescription="Logotip Članarina",
                        modifier=Modifier.size(35.dp))
                    Text("Članarina",fontWeight=FontWeight.Bold)
                }
            },
            actions={
                Box {
                    TextButton(onClick={menu=true}) { Text("Moduli",color=Color.White) }
                    DropdownMenu(expanded=menu,onDismissRequest={menu=false}) {
                        sections.forEach { name -> DropdownMenuItem(text={Text(name)},onClick={page=name;menu=false}) }
                    }
                }
            },
            colors=TopAppBarDefaults.topAppBarColors(containerColor=Brand.Navy,titleContentColor=Color.White)
        )},
        bottomBar={ NavigationBar {
            listOf("Početna","Članovi","Uplate","Više").forEach { tab ->
                NavigationBarItem(
                    selected=(page==tab || (tab=="Više" && page !in listOf("Početna","Članovi","Uplate"))),
                    onClick={page=if(tab=="Više") "Članarine" else tab},
                    icon={Text(when(tab){"Početna"->"⌂";"Članovi"->"●";"Uplate"->"✓";else->"⋯"})},
                    label={Text(tab)}
                )
            }
        }}
    ) { inner ->
        Column(Modifier.fillMaxSize().padding(inner).padding(horizontal=16.dp)) {
            Spacer(Modifier.height(16.dp))
            Text(page,style=MaterialTheme.typography.headlineMedium,fontWeight=FontWeight.Bold)
            Spacer(Modifier.height(14.dp))
            when(page) {
                "Početna" -> Column(Modifier.verticalScroll(rememberScrollState())) {
                    Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                        StatCard("Članovi",totals.members.toString(),Brand.Blue,Modifier.weight(1f))
                        StatCard("Uplaćeno",euro(totals.paidCents),Brand.Teal,Modifier.weight(1f))
                    }
                    Row(horizontalArrangement=Arrangement.spacedBy(8.dp)) {
                        StatCard("Zaduženo",euro(totals.chargedCents),Brand.Gold,Modifier.weight(1f))
                        StatCard("Dugovanja",euro(totals.outstandingCents),Brand.Danger,Modifier.weight(1f))
                    }
                    Spacer(Modifier.height(16.dp))
                    Text("Brze radnje",fontWeight=FontWeight.Bold)
                    Button(onClick={form="member"}) { Text("Novi član") }
                    OutlinedButton(onClick={page="Uplate";form="payment"},enabled=members.isNotEmpty()) { Text("Nova uplata") }
                    Text("Podaci se čuvaju samo na ovom uređaju.",style=MaterialTheme.typography.bodySmall)
                }
                "Članovi","Dugovanja" -> MemberManagement(db, page=="Dugovanja", changes) { changes++ }
                "Članarine","Uplate" -> LedgerManagement(
                    db, if(page=="Članarine") "charge" else "payment", changes
                ) { changes++ }
                "Izvještaji" -> Column(Modifier.verticalScroll(rememberScrollState())) {
                    StatCard("Broj članova",totals.members.toString(),Brand.Blue)
                    StatCard("Zaduženja",euro(totals.chargedCents),Brand.Gold)
                    StatCard("Evidentirane uplate",euro(totals.paidCents),Brand.Teal)
                    StatCard("Nepodmireno",euro(totals.outstandingCents),Brand.Danger)
                    StatCard("Preplate",euro(totals.creditCents),Brand.Blue)
                }
                "Licenca" -> Empty("Online i offline aktivacija licence još nije povezana s aplikacijom.")
                "Uplatnice" -> Empty("HUB-3 i EPC QR još nisu implementirani ni verificirani.")
                "Bankovni izvodi" -> Empty("Uvoz i usklađivanje bankovnih izvoda još nisu implementirani.")
                "Postavke" -> Empty("Lokalna SQLite pohrana. Bez računa, clouda i praćenja korisnika.")
            }
        }
    }
    if(form=="member") {
        var name by remember { mutableStateOf("") }
        var group by remember { mutableStateOf("") }
        var email by remember { mutableStateOf("") }
        AlertDialog(onDismissRequest={form="";error=""},title={Text("Novi član")},
            text={Column(verticalArrangement=Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(value=name,onValueChange={name=it},label={Text("Ime i prezime")})
                OutlinedTextField(value=group,onValueChange={group=it},label={Text("Grupa")})
                OutlinedTextField(value=email,onValueChange={email=it},label={Text("E-mail (neobavezno)")})
                if(error.isNotEmpty()) Text(error,color=MaterialTheme.colorScheme.error)
            }},
            confirmButton={TextButton(onClick={
                try { db.addMember(name,group,email);changes++;form="";error="" }
                catch(e:Exception) { error=e.message ?: "Neispravni podaci" }
            }) { Text("Spremi") }},
            dismissButton={TextButton(onClick={form="";error=""}) { Text("Odustani") }}
        )
    }
    if(form=="charge" || form=="payment") {
        val kind=form
        var memberId by remember { mutableStateOf(members.firstOrNull()?.id ?: "") }
        var amount by remember { mutableStateOf("") }
        AlertDialog(onDismissRequest={form="";error=""},title={Text(if(kind=="charge")"Novo zaduženje" else "Nova uplata")},
            text={Column {
                Text("Odaberite člana")
                Column(Modifier.heightIn(max=160.dp).verticalScroll(rememberScrollState())) {
                    members.forEach { m -> Row(verticalAlignment=Alignment.CenterVertically) {
                        RadioButton(selected=memberId==m.id,onClick={memberId=m.id})
                        Text(m.name)
                    } }
                }
                OutlinedTextField(value=amount,onValueChange={amount=it},label={Text("Iznos (€)")},
                    keyboardOptions=KeyboardOptions(keyboardType=KeyboardType.Decimal))
                if(error.isNotEmpty()) Text(error,color=MaterialTheme.colorScheme.error)
            }},
            confirmButton={TextButton(onClick={
                try {
                    val value=parseEuro(amount)
                    if(kind=="charge") db.addCharge(memberId,value) else db.addPayment(memberId,value)
                    changes++;form="";error=""
                } catch(e:Exception) { error="Provjerite člana i pozitivan iznos (npr. 25,00)." }
            }) { Text("Spremi") }},
            dismissButton={TextButton(onClick={form="";error=""}) { Text("Odustani") }}
        )
    }
}
@Composable private fun StatCard(title:String,value:String,accent:Color,modifier:Modifier=Modifier) {
    Card(modifier=modifier.padding(vertical=4.dp),shape=RoundedCornerShape(16.dp),colors=CardDefaults.cardColors(containerColor=Color.White)) {
        Column(Modifier.padding(15.dp)) {
            Text(title,color=accent,style=MaterialTheme.typography.bodySmall,fontWeight=FontWeight.SemiBold)
            Spacer(Modifier.height(8.dp))
            Text(value,style=MaterialTheme.typography.titleLarge,fontWeight=FontWeight.Bold)
        }
    }
}
@Composable private fun MemberTile(member:Member) {
    Card(colors=CardDefaults.cardColors(containerColor=Color.White),shape=RoundedCornerShape(14.dp)) {
        Column(Modifier.fillMaxWidth().padding(16.dp)) {
            Text(member.name,fontWeight=FontWeight.Bold)
            Text(if(member.group.isBlank())"Bez grupe" else member.group,style=MaterialTheme.typography.bodySmall)
            Text("Zaduženo: " + euro(member.dueCents) + "  ·  Uplaćeno: " + euro(member.paidCents))
            Text("Saldo: " + euro(member.balanceCents),color=if(member.balanceCents>0) Brand.Danger else Brand.Teal)
        }
    }
}
@Composable private fun Empty(text:String) {
    Card(colors=CardDefaults.cardColors(containerColor=Color.White)) {
        Text(text,Modifier.fillMaxWidth().padding(22.dp))
    }
}
