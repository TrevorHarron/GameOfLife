extends TileMap

class_name World


func clear_world():
	clear()

func _on_main_update_cell(x, y, isAlive):
	var cell = Vector2i(x,y)
	if (isAlive):
		set_cell(0, cell,0,Vector2i.ZERO)
	else:
		erase_cell(0, cell)
