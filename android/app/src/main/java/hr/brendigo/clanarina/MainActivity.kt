package hr.brendigo.clanarina

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import hr.brendigo.clanarina.data.LocalStore
import hr.brendigo.clanarina.ui.ClanarinaTheme

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
internal fun DatabaseRecovery(onRetry: () -> Unit) {
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
